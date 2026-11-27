using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using Repos;
using WebApiDto.SmartHome;

namespace passi_webapi.SmartHome;

public sealed class SmartHomeException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public SmartHomeException(string code, int statusCode = 400) : base(code)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

// Never serialize this identity or return it to the browser/agent.
public sealed class AgentIdentity
{
    public Guid HomeId { get; set; }
    public Guid OwnerId { get; set; }
    public string TokenHash { get; set; }
    public long Generation { get; set; }
}

public sealed partial class SmartHomeService
{
    private readonly PassiDbContext db;
    private readonly TimeProvider clock;
    private static readonly string[] Capabilities = { "power.read", "temperature.read", "power.set" };
    public SmartHomeService(PassiDbContext db, TimeProvider clock)
    {
        this.db = db;
        this.clock = clock;
    }

    // All state transitions, including credential checks and command claims, share a PostgreSQL
    // transaction lock. This intentionally serializes the small, single-owner feature across replicas.
    // No command is sent outside a committed transaction; ambiguous commit retries cannot redeliver it.
    private async Task<T> Atomic<T>(Func<Task<T>> action, CancellationToken ct)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(72417022153401)", ct);
            var result = await action();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        });
    }

    public Task<Guid?> ResolveOwnerAsync(string validatedSubject, string thumbprint, CancellationToken ct = default) => Atomic<Guid?>(async () =>
    {
        if (string.IsNullOrWhiteSpace(validatedSubject) || string.IsNullOrWhiteSpace(thumbprint)) return null;
        var user = await db.Users.SingleOrDefaultAsync(x => x.EmailHash == validatedSubject &&
            x.Guid != Guid.Empty && x.Invitations.Any(i => i.IsConfirmed) &&
            x.Certificates.Any(c => c.Thumbprint == thumbprint && !c.IsRetired), ct);
        if (user == null) return null;
        var pin = await db.SmartHomeOwners.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (pin == null)
        {
            if (user.EmailHash != "admin@passi.cloud") return null;
            pin = new SmartHomeOwnerDb { Id = 1, OwnerId = user.Guid };
            db.SmartHomeOwners.Add(pin);
        }
        return pin.OwnerId == user.Guid ? user.Guid : null;
    }, ct);

    private async Task CheckOwner(Guid owner, CancellationToken ct)
    {
        if (!await db.SmartHomeOwners.AnyAsync(x => x.Id == 1 && x.OwnerId == owner, ct) ||
            !await db.Users.AnyAsync(x => x.Guid == owner, ct))
            throw new SmartHomeException("forbidden", 403);
    }

    private async Task<SmartHomeDb> OwnerHome(Guid owner, Guid homeId, CancellationToken ct)
    {
        await CheckOwner(owner, ct);
        return await db.SmartHomes.SingleOrDefaultAsync(x => x.Id == homeId && x.OwnerId == owner, ct)
            ?? throw new SmartHomeException("home_not_found", 404);
    }

    public Task<DashboardDto> GetDashboardAsync(Guid owner, CancellationToken ct = default) => Atomic(async () =>
    {
        await CheckOwner(owner, ct);
        var now = clock.GetUtcNow();
        var homes = await db.SmartHomes.Where(x => x.OwnerId == owner).OrderBy(x => x.Name).ToListAsync(ct);
        var dashboard = new DashboardDto { ServerTime = now };
        foreach (var home in homes)
        {
            await Expire(home, now, ct);
            var commands = await db.SmartHomeCommands.Where(x => x.HomeId == home.Id)
                .OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct);
            dashboard.Homes.Add(new HomeDto
            {
                Id = home.Id, Name = home.Name, Online = Online(home, now), LastSeenAt = home.LastSeenAt,
                SnapshotAt = home.SnapshotAt, Stale = !Fresh(home, now), Devices = Devices(home),
                Commands = commands.Select(ToDto).ToList(), Schedules = ScheduleViews(home, now)
            });
        }
        return dashboard;
    }, ct);

    public Task<EnrollmentDto> CreateHomeAsync(Guid owner, string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80 || name.Any(char.IsControl))
            throw new SmartHomeException("invalid_name");
        var id = Guid.NewGuid();
        var secret = Secret();
        return Atomic(async () =>
        {
            await CheckOwner(owner, ct);
            if (await db.SmartHomes.CountAsync(x => x.OwnerId == owner, ct) >= 10)
                throw new SmartHomeException("home_limit", 409);
            var home = new SmartHomeDb { Id = id, OwnerId = owner, Name = name.Trim(), Generation = 1 };
            db.SmartHomes.Add(home);
            return NewEnrollment(home, secret, clock.GetUtcNow());
        }, ct);
    }

    public Task<EnrollmentDto> ReenrollAsync(Guid owner, Guid homeId, CancellationToken ct = default)
    {
        var secret = Secret();
        return Atomic(async () =>
        {
            var home = await OwnerHome(owner, homeId, ct);
            await Invalidate(home, clock.GetUtcNow(), ct);
            return NewEnrollment(home, secret, clock.GetUtcNow());
        }, ct);
    }

    public Task RevokeAsync(Guid owner, Guid homeId, CancellationToken ct = default) => Atomic(async () =>
    {
        var home = await OwnerHome(owner, homeId, ct);
        await Invalidate(home, clock.GetUtcNow(), ct);
        home.EnrollmentHash = null;
        home.EnrollmentExpiresAt = null;
        return true;
    }, ct);

    private static EnrollmentDto NewEnrollment(SmartHomeDb home, string secret, DateTimeOffset now)
    {
        home.EnrollmentHash = Hash(secret);
        home.EnrollmentExpiresAt = now.AddMinutes(10);
        return new EnrollmentDto { HomeId = home.Id, EnrollmentSecret = secret, ExpiresAt = home.EnrollmentExpiresAt.Value };
    }

    public Task<AgentTokenDto> EnrollAsync(Guid homeId, string enrollmentSecret, CancellationToken ct = default)
    {
        var token = Secret();
        return Atomic(async () =>
        {
            var home = await db.SmartHomes.SingleOrDefaultAsync(x => x.Id == homeId, ct);
            var now = clock.GetUtcNow();
            if (home == null || home.EnrollmentExpiresAt <= now || home.EnrollmentExpiresAt == null ||
                !EqualHash(home.EnrollmentHash, enrollmentSecret))
                throw new SmartHomeException("invalid_enrollment", 401);
            await CheckOwner(home.OwnerId, ct);
            home.EnrollmentHash = null;
            home.EnrollmentExpiresAt = null;
            home.TokenHash = Hash(token);
            home.TokenExpiresAt = now.AddDays(90);
            return new AgentTokenDto { HomeId = home.Id, Token = token, ExpiresAt = home.TokenExpiresAt.Value };
        }, ct);
    }

    public Task<AgentIdentity> AuthenticateAgentAsync(string token, CancellationToken ct = default) => Atomic(async () =>
    {
        if (!TokenSyntax(token)) return null;
        var hash = Hash(token);
        var home = await db.SmartHomes.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (home == null || home.TokenExpiresAt <= clock.GetUtcNow() || home.TokenExpiresAt == null) return null;
        if (!await db.SmartHomeOwners.AnyAsync(x => x.Id == 1 && x.OwnerId == home.OwnerId, ct) ||
            !await db.Users.AnyAsync(x => x.Guid == home.OwnerId, ct)) return null;
        return new AgentIdentity { HomeId = home.Id, OwnerId = home.OwnerId, TokenHash = hash, Generation = home.Generation };
    }, ct);

    private async Task<SmartHomeDb> AgentHome(AgentIdentity identity, CancellationToken ct)
    {
        if (identity == null) throw new SmartHomeException("invalid_agent", 401);
        var home = await db.SmartHomes.SingleOrDefaultAsync(x => x.Id == identity.HomeId, ct);
        if (home == null || home.OwnerId != identity.OwnerId || home.Generation != identity.Generation ||
            home.TokenHash == null || !FixedEquals(home.TokenHash, identity.TokenHash) ||
            home.TokenExpiresAt == null || home.TokenExpiresAt <= clock.GetUtcNow())
            throw new SmartHomeException("invalid_agent", 401);
        await CheckOwner(home.OwnerId, ct);
        return home;
    }

    public Task ConnectAsync(AgentIdentity identity, string connectionId, CancellationToken ct = default) => Atomic(async () =>
    {
        if (string.IsNullOrEmpty(connectionId) || connectionId.Length > 128) throw new SmartHomeException("invalid_connection");
        var home = await AgentHome(identity, ct);
        var now = clock.GetUtcNow();
        await EndPending(home, now, ct);
        home.ConnectionId = connectionId;
        home.ConnectedAt = now;
        home.LastSeenAt = now;
        // Keep the old readings for display, but require an exchange from this connection for control.
        home.AwaitingSnapshot = true;
        return true;
    }, ct);

    public Task DisconnectAsync(AgentIdentity identity, string connectionId, CancellationToken ct = default) => Atomic(async () =>
    {
        var home = await db.SmartHomes.SingleOrDefaultAsync(x => x.Id == identity.HomeId, ct);
        if (home == null || home.ConnectionId != connectionId || home.Generation != identity.Generation) return false;
        await EndPending(home, clock.GetUtcNow(), ct);
        home.ConnectionId = null;
        return true;
    }, ct);

    public Task<CommandDto> QueueCommandAsync(Guid owner, Guid homeId, string deviceId, PowerCommandRequest request, CancellationToken ct = default) => Atomic(async () =>
    {
        var home = await OwnerHome(owner, homeId, ct);
        if (request == null || request.CommandId == Guid.Empty || !Identifier(deviceId))
            throw new SmartHomeException("invalid_command");
        var existing = await db.SmartHomeCommands.SingleOrDefaultAsync(x => x.Id == request.CommandId, ct);
        if (existing != null)
        {
            if (existing.HomeId != homeId || existing.DeviceId != deviceId || existing.Operation != "power.set" || existing.On != request.On)
                throw new SmartHomeException("command_id_conflict", 409);
            await Expire(home, clock.GetUtcNow(), ct);
            return ToDto(existing);
        }
        var now = clock.GetUtcNow();
        await Expire(home, now, ct);
        if (!Fresh(home, now)) throw new SmartHomeException("home_offline_or_stale", 409);
        var device = Devices(home).SingleOrDefault(x => x.Id == deviceId);
        if (device == null || !device.Capabilities.Contains("power.set") || !device.Capabilities.Contains("power.read"))
            throw new SmartHomeException("unsupported_device_operation", 400);
        if (!device.Online || device.Power == null || device.UpdatedAt < now.AddSeconds(-30))
            throw new SmartHomeException("device_offline_or_stale", 409);
        if (await db.SmartHomeCommands.AnyAsync(x => x.HomeId == homeId && x.DeviceId == deviceId &&
            (x.Status == "queued" || x.Status == "sent") && x.ExpiresAt > now, ct))
            throw new SmartHomeException("command_in_progress", 409);
        var command = new SmartHomeCommandDb
        {
            Id = request.CommandId, HomeId = homeId, DeviceId = deviceId, On = request.On,
            CreatedAt = now, ExpiresAt = now.AddSeconds(30), Status = "queued", ConnectionId = home.ConnectionId
        };
        db.SmartHomeCommands.Add(command);
        return ToDto(command);
    }, ct);

    public Task<ExchangeResponse> ExchangeAsync(AgentIdentity identity, string connectionId, ExchangeRequest request, CancellationToken ct = default) => Atomic(async () =>
    {
        var home = await AgentHome(identity, ct);
        if (home.ConnectionId != connectionId) throw new SmartHomeException("connection_replaced", 409);
        var now = clock.GetUtcNow();
        ValidateSnapshot(home, request, now);
        // Expire BEFORE renewing the heartbeat: silence must not resurrect queued work.
        await Expire(home, now, ct);
        home.LastSeenAt = now;
        home.SnapshotAt = request.SnapshotAt.ToUniversalTime();
        home.AwaitingSnapshot = false;
        home.DevicesJson = JsonSerializer.Serialize(request.Devices);
        InvalidateAbsentSchedules(home);
        foreach (var result in request.Results)
        {
            var command = await db.SmartHomeCommands.SingleOrDefaultAsync(x => x.Id == result.CommandId && x.HomeId == home.Id, ct);
            if (command == null) throw new SmartHomeException("unknown_command", 404);
            if (command.Operation != "power.set") throw new SmartHomeException("command_operation_mismatch", 409);
            if (command.ConnectionId != connectionId) throw new SmartHomeException("command_connection_mismatch", 409);
            // Terminal records are immutable; retries of an acknowledgement are harmless.
            if (command.Status != "sent") continue;
            var device = request.Devices.SingleOrDefault(x => x.Id == command.DeviceId);
            var readback = result.Power.HasValue && result.ObservedAt.HasValue &&
                command.SentAt.HasValue && result.ObservedAt >= command.SentAt && result.ObservedAt <= now.AddSeconds(5) &&
                device != null && device.Online && device.UpdatedAt >= result.ObservedAt &&
                device.Power == result.Power;
            if (result.Status == "confirmed")
                command.Status = !readback ? "unknown" : result.Power == command.On ? "confirmed" : "failed";
            else command.Status = result.Status;
            command.CompletedAt = now;
            command.ErrorCode = command.Status == "confirmed" ? null : result.ErrorCode ??
                (command.Status == "unknown" ? "unconfirmed_result" : "device_rejected");
        }
        var pending = await db.SmartHomeCommands.Where(x => x.HomeId == home.Id && x.Status == "queued" && x.Operation == "power.set")
            .OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var response = new ExchangeResponse { ServerTime = now };
        foreach (var command in pending)
        {
            // The query may include tracked rows expired earlier in this transaction.
            if (command.Status != "queued") continue;
            var device = request.Devices.SingleOrDefault(x => x.Id == command.DeviceId);
            if (command.ExpiresAt <= now || command.ConnectionId != connectionId || device == null ||
                !device.Online || device.UpdatedAt < now.AddSeconds(-30) || device.Power == null ||
                !device.Capabilities.Contains("power.set") || !device.Capabilities.Contains("power.read"))
            {
                command.Status = "expired";
                command.CompletedAt = now;
                command.ErrorCode = "device_unavailable";
                continue;
            }
            command.Status = "sent";
            command.SentAt = now;
            response.Commands.Add(ToDto(command));
        }
        return response;
    }, ct);

    private static void ValidateSnapshot(SmartHomeDb home, ExchangeRequest request, DateTimeOffset now)
    {
        if (request == null || request.Devices == null || request.Results == null || request.Devices.Count > 32 || request.Results.Count > 64 ||
            request.SnapshotAt < now.AddSeconds(-30) || request.SnapshotAt > now.AddSeconds(5) ||
            request.SnapshotAt < home.ConnectedAt || (home.SnapshotAt.HasValue && request.SnapshotAt < home.SnapshotAt))
            throw new SmartHomeException("invalid_or_stale_snapshot");
        if (request.Devices.Any(x => x == null) || request.Devices.Select(x => x.Id).Distinct().Count() != request.Devices.Count)
            throw new SmartHomeException("invalid_devices");
        foreach (var device in request.Devices)
        {
            var previous = Devices(home).SingleOrDefault(x => x.Id == device.Id);
            if (!Identifier(device.Id) || string.IsNullOrWhiteSpace(device.Name) || device.Name.Length > 80 ||
                device.Name.Any(char.IsControl) || device.Model?.Length > 80 ||
                device.Capabilities == null || device.Capabilities.Length > 3 ||
                device.Capabilities.Distinct().Count() != device.Capabilities.Length ||
                device.Capabilities.Any(x => !Capabilities.Contains(x)) ||
                device.UpdatedAt > request.SnapshotAt || device.UpdatedAt > now.AddSeconds(5) ||
                device.UpdatedAt < DateTimeOffset.UnixEpoch ||
                (previous != null && device.UpdatedAt < previous.UpdatedAt) ||
                (device.Power.HasValue && !device.Capabilities.Contains("power.read")) ||
                (device.TemperatureCelsius.HasValue && (!double.IsFinite(device.TemperatureCelsius.Value) ||
                    device.TemperatureCelsius < -100 || device.TemperatureCelsius > 250 || !device.Capabilities.Contains("temperature.read"))))
                throw new SmartHomeException("invalid_device");
        }
        if (request.Results.Any(r => r == null || r.CommandId == Guid.Empty ||
            (r.Status != "confirmed" && r.Status != "failed" && r.Status != "unknown") ||
            (r.ErrorCode != null && !Identifier(r.ErrorCode)))) throw new SmartHomeException("invalid_result");
        if (request.Results.Select(x => x.CommandId).Distinct().Count() != request.Results.Count)
            throw new SmartHomeException("duplicate_result");
    }

    private async Task Invalidate(SmartHomeDb home, DateTimeOffset now, CancellationToken ct)
    {
        await EndPending(home, now, ct);
        home.TokenHash = null;
        home.TokenExpiresAt = null;
        home.ConnectionId = null;
        home.Generation++;
    }

    private async Task EndPending(SmartHomeDb home, DateTimeOffset now, CancellationToken ct)
    {
        var pending = await db.SmartHomeCommands.Where(x => x.HomeId == home.Id && (x.Status == "queued" || x.Status == "sent")).ToListAsync(ct);
        foreach (var command in pending)
        {
            if (command.Status != "queued" && command.Status != "sent") continue;
            command.Status = command.Status == "sent" ? "unknown" : "expired";
            command.CompletedAt = now;
            command.ErrorCode = "connection_lost";
        }
    }

    private async Task Expire(SmartHomeDb home, DateTimeOffset now, CancellationToken ct)
    {
        if (!Online(home, now))
        {
            await EndPending(home, now, ct);
            return;
        }
        var commands = await db.SmartHomeCommands.Where(x => x.HomeId == home.Id &&
            (x.Status == "queued" || x.Status == "sent") && x.ExpiresAt <= now).ToListAsync(ct);
        foreach (var command in commands)
        {
            command.Status = command.Status == "sent" ? "unknown" : "expired";
            command.CompletedAt = now;
            command.ErrorCode = "command_timeout";
        }
    }

    private static bool Online(SmartHomeDb home, DateTimeOffset now) => home.ConnectionId != null &&
        home.TokenHash != null && home.TokenExpiresAt > now && home.LastSeenAt >= now.AddSeconds(-20);
    private static bool Fresh(SmartHomeDb home, DateTimeOffset now) => Online(home, now) &&
        !home.AwaitingSnapshot && home.SnapshotAt >= home.ConnectedAt && home.SnapshotAt >= now.AddSeconds(-30);
    private static List<DeviceDto> Devices(SmartHomeDb home) => JsonSerializer.Deserialize<List<DeviceDto>>(home.DevicesJson) ?? new();
    private static string Secret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static bool TokenSyntax(string token) => token?.Length == 43 && token.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static bool EqualHash(string expected, string token) => expected != null && TokenSyntax(token) && FixedEquals(expected, Hash(token));
    private static bool FixedEquals(string a, string b) => a != null && b != null && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));
    private static bool Identifier(string value) => value != null && value.Length is > 0 and <= 80 &&
        Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9_.:-]*$", RegexOptions.CultureInvariant);
    private static CommandDto ToDto(SmartHomeCommandDb command)
    {
        var payload = command.PayloadJson == null ? null : JsonSerializer.Deserialize<ScheduleCommandPayload>(command.PayloadJson);
        return new CommandDto
        {
            Id = command.Id, HomeId = command.HomeId, DeviceId = command.DeviceId, On = command.On, Operation = command.Operation,
            CreatedAt = command.CreatedAt, ExpiresAt = command.ExpiresAt, SentAt = command.SentAt, Status = command.Status,
            CompletedAt = command.CompletedAt, ErrorCode = command.ErrorCode,
            ExpectedVersion = payload?.ExpectedVersion, ExpectedRuntimeRevision = payload?.ExpectedRuntimeRevision,
            Configuration = payload?.Configuration
        };
    }
}
