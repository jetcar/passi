using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using NodaTime;
using WebApiDto.SmartHome;

namespace passi_webapi.SmartHome;

public sealed partial class SmartHomeService
{
    public const long MaximumScheduleVersion = 9007199254740991;
    private sealed class ScheduleCache
    {
        public string DeviceId { get; set; }
        public string ConnectionId { get; set; }
        public DateTimeOffset SnapshotAt { get; set; }
        public ScheduleConfigurationDto Configuration { get; set; }
        public ScheduleRuntimeDto Runtime { get; set; }
    }
    private sealed class ScheduleCommandPayload
    {
        public long ExpectedVersion { get; set; }
        public long? ExpectedRuntimeRevision { get; set; }
        public ScheduleConfigurationDto Configuration { get; set; }
        // Resume must not acknowledge a different configuration at the same version.
        public ScheduleConfigurationDto BaselineConfiguration { get; set; }
    }

    public Task<CommandDto> ReplaceScheduleAsync(Guid owner, Guid homeId, string deviceId,
        ReplaceScheduleRequest request, CancellationToken ct = default) => Atomic(async () =>
    {
        var home = await OwnerHome(owner, homeId, ct);
        if (request == null || request.CommandId == Guid.Empty || !Identifier(deviceId) ||
            request.ExpectedVersion < 0 || request.ExpectedVersion >= MaximumScheduleVersion)
            throw new SmartHomeException("invalid_schedule");
        ValidateSchedule(request.Configuration, deviceId);
        if (request.Configuration.Version != request.ExpectedVersion) throw new SmartHomeException("version_conflict", 409);
        var payload = new ScheduleCommandPayload
        {
            ExpectedVersion = request.ExpectedVersion,
            Configuration = CanonicalConfiguration(request.Configuration)
        };
        return await QueueSchedule(home, deviceId, request.CommandId, "schedule.replace", payload, ct);
    }, ct);

    public Task<CommandDto> ResumeScheduleAsync(Guid owner, Guid homeId, string deviceId,
        ResumeScheduleRequest request, CancellationToken ct = default) => Atomic(async () =>
    {
        var home = await OwnerHome(owner, homeId, ct);
        if (request == null || request.CommandId == Guid.Empty || !Identifier(deviceId) ||
            !ValidVersion(request.ExpectedVersion) || request.ExpectedRuntimeRevision < 0 ||
            request.ExpectedRuntimeRevision >= MaximumScheduleVersion)
            throw new SmartHomeException("invalid_schedule");
        return await QueueSchedule(home, deviceId, request.CommandId, "schedule.resume", new ScheduleCommandPayload
        {
            ExpectedVersion = request.ExpectedVersion, ExpectedRuntimeRevision = request.ExpectedRuntimeRevision
        }, ct);
    }, ct);

    private async Task<CommandDto> QueueSchedule(SmartHomeDb home, string deviceId, Guid commandId,
        string operation, ScheduleCommandPayload payload, CancellationToken ct)
    {
        var existing = await db.SmartHomeCommands.SingleOrDefaultAsync(x => x.Id == commandId, ct);
        if (existing != null)
        {
            if (existing.HomeId != home.Id || existing.DeviceId != deviceId || existing.Operation != operation ||
                !SameIntent(JsonSerializer.Deserialize<ScheduleCommandPayload>(existing.PayloadJson), payload))
                throw new SmartHomeException("command_id_conflict", 409);
            await Expire(home, clock.GetUtcNow(), ct);
            return ToDto(existing);
        }
        var now = clock.GetUtcNow();
        await Expire(home, now, ct);
        var snapshot = Schedules(home).SingleOrDefault(x => x.DeviceId == deviceId);
        if (snapshot == null) throw new SmartHomeException("schedule_not_supported", 409);
        if (!ScheduleFresh(home, snapshot, now)) throw new SmartHomeException("schedule_offline_or_stale", 409);
        if (snapshot.Configuration.Version != payload.ExpectedVersion) throw new SmartHomeException("version_conflict", 409);
        if (operation == "schedule.resume")
        {
            if (snapshot.Runtime.Revision != payload.ExpectedRuntimeRevision) throw new SmartHomeException("runtime_revision_conflict", 409);
            if (!snapshot.Runtime.ManualActive) throw new SmartHomeException("manual_override_not_active", 409);
            payload.BaselineConfiguration = snapshot.Configuration;
        }
        if (await db.SmartHomeCommands.AnyAsync(x => x.HomeId == home.Id && x.DeviceId == deviceId &&
            (x.Status == "queued" || x.Status == "sent") && x.ExpiresAt > now, ct))
            throw new SmartHomeException("command_in_progress", 409);
        var command = new SmartHomeCommandDb
        {
            Id = commandId, HomeId = home.Id, DeviceId = deviceId, Operation = operation,
            PayloadJson = JsonSerializer.Serialize(payload), Status = "queued", ConnectionId = home.ConnectionId,
            CreatedAt = now, ExpiresAt = now.AddSeconds(30)
        };
        db.SmartHomeCommands.Add(command);
        return ToDto(command);
    }

    public Task<ExchangeResponse> ExchangeScheduleAsync(AgentIdentity identity, string connectionId,
        ExchangeScheduleRequest request, CancellationToken ct = default) => Atomic(async () =>
    {
        var home = await AgentHome(identity, ct);
        if (home.ConnectionId != connectionId) throw new SmartHomeException("connection_replaced", 409);
        var now = clock.GetUtcNow();
        var cache = Schedules(home);
        ValidateScheduleSnapshot(home, cache, request, now);
        await Expire(home, now, ct);
        var snapshot = new ScheduleCache
        {
            DeviceId = request.DeviceId, ConnectionId = connectionId, SnapshotAt = request.SnapshotAt.ToUniversalTime(),
            Configuration = CanonicalConfiguration(request.Configuration), Runtime = request.Runtime
        };
        // Historical entries preserve version/revision monotonicity when an inventory entry disappears
        // and returns. Never silently discard those tombstones to admit more device identifiers.
        if (cache.All(x => x.DeviceId != request.DeviceId) && cache.Count >= 128)
            throw new SmartHomeException("schedule_cache_limit", 409);
        cache.RemoveAll(x => x.DeviceId == request.DeviceId);
        cache.Add(snapshot);
        home.SchedulesJson = JsonSerializer.Serialize(cache);
        foreach (var result in request.Results)
        {
            var command = await db.SmartHomeCommands.SingleOrDefaultAsync(x => x.Id == result.CommandId &&
                x.HomeId == home.Id && x.DeviceId == request.DeviceId, ct);
            if (command == null) throw new SmartHomeException("unknown_command", 404);
            if (command.Operation != "schedule.replace" && command.Operation != "schedule.resume")
                throw new SmartHomeException("command_operation_mismatch", 409);
            if (command.ConnectionId != connectionId) throw new SmartHomeException("command_connection_mismatch", 409);
            if (command.Status != "sent") continue;
            var payload = JsonSerializer.Deserialize<ScheduleCommandPayload>(command.PayloadJson);
            var proof = command.SentAt.HasValue && result.ObservedAt.HasValue &&
                result.ObservedAt >= command.SentAt && result.ObservedAt <= snapshot.SnapshotAt &&
                result.AppliedVersion == snapshot.Configuration.Version && result.RuntimeRevision == snapshot.Runtime.Revision;
            if (command.Operation == "schedule.replace")
                proof &= snapshot.Configuration.Version == payload.ExpectedVersion + 1 &&
                    SameConfiguration(snapshot.Configuration, payload.Configuration, ignoreVersion: true);
            else
                proof &= snapshot.Configuration.Version == payload.ExpectedVersion &&
                    SameConfiguration(snapshot.Configuration, payload.BaselineConfiguration) &&
                    snapshot.Runtime.Revision == payload.ExpectedRuntimeRevision + 1 &&
                    !snapshot.Runtime.ManualActive && snapshot.Runtime.ManualUntil == null;
            command.Status = result.Status == "confirmed" ? proof ? "confirmed" : "unknown" : result.Status;
            command.CompletedAt = now;
            command.ErrorCode = command.Status == "confirmed" ? null : result.ErrorCode ??
                (command.Status == "unknown" ? "unconfirmed_result" : "schedule_rejected");
        }
        var pending = await db.SmartHomeCommands.Where(x => x.HomeId == home.Id && x.DeviceId == request.DeviceId &&
            x.Status == "queued" && (x.Operation == "schedule.replace" || x.Operation == "schedule.resume"))
            .OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var response = new ExchangeResponse { ServerTime = now };
        foreach (var command in pending)
        {
            if (command.Status != "queued") continue;
            var payload = JsonSerializer.Deserialize<ScheduleCommandPayload>(command.PayloadJson);
            if (command.ExpiresAt <= now || command.ConnectionId != connectionId || !ScheduleFresh(home, snapshot, now))
            {
                command.Status = "expired";
                command.CompletedAt = now;
                command.ErrorCode = "schedule_unavailable";
            }
            else if (snapshot.Configuration.Version != payload.ExpectedVersion ||
                (command.Operation == "schedule.resume" && (snapshot.Runtime.Revision != payload.ExpectedRuntimeRevision || !snapshot.Runtime.ManualActive)))
            {
                command.Status = "failed";
                command.CompletedAt = now;
                command.ErrorCode = snapshot.Configuration.Version != payload.ExpectedVersion ? "version_conflict" : "runtime_revision_conflict";
            }
            else
            {
                command.Status = "sent";
                command.SentAt = now;
                response.Commands.Add(ToDto(command));
                break;
            }
        }
        return response;
    }, ct);

    private static void ValidateScheduleSnapshot(SmartHomeDb home, List<ScheduleCache> cache,
        ExchangeScheduleRequest request, DateTimeOffset now)
    {
        if (request == null || request.ProtocolVersion != 1) throw new SmartHomeException("unsupported_schedule_protocol");
        if (!Identifier(request.DeviceId)) throw new SmartHomeException("invalid_device");
        var device = Devices(home).SingleOrDefault(x => x.Id == request.DeviceId);
        if (device == null || !device.Capabilities.Contains("power.read") || !device.Capabilities.Contains("power.set"))
            throw new SmartHomeException("unsupported_device_operation");
        if (!Fresh(home, now)) throw new SmartHomeException("home_offline_or_stale", 409);
        var old = cache.SingleOrDefault(x => x.DeviceId == request.DeviceId);
        if (request.SnapshotAt < now.AddSeconds(-30) || request.SnapshotAt > now.AddSeconds(5) ||
            request.SnapshotAt < home.ConnectedAt || (old != null && request.SnapshotAt < old.SnapshotAt))
            throw new SmartHomeException("invalid_or_stale_snapshot");
        ValidateSchedule(request.Configuration, request.DeviceId);
        var runtime = request.Runtime;
        if (runtime == null || !ValidVersion(runtime.Revision) || !Identifier(runtime.State) || runtime.State.Length > 40 ||
            (runtime.Error != null && !Identifier(runtime.Error)) ||
            (runtime.ActualPower.HasValue && !runtime.CheckedAt.HasValue) ||
            runtime.CheckedAt > request.SnapshotAt || runtime.CheckedAt < DateTimeOffset.UnixEpoch ||
            (!runtime.ManualActive && runtime.ManualUntil.HasValue))
            throw new SmartHomeException("invalid_schedule_runtime");
        if (old != null)
        {
            if (request.Configuration.Version < old.Configuration.Version || runtime.Revision < old.Runtime.Revision ||
                (old.Runtime.CheckedAt.HasValue && runtime.CheckedAt.HasValue && runtime.CheckedAt < old.Runtime.CheckedAt))
                throw new SmartHomeException("stale_schedule_version", 409);
            if (request.Configuration.Version == old.Configuration.Version && !SameConfiguration(request.Configuration, old.Configuration))
                throw new SmartHomeException("schedule_version_content_conflict", 409);
            if (runtime.Revision == old.Runtime.Revision &&
                (runtime.ManualActive != old.Runtime.ManualActive || runtime.ManualUntil != old.Runtime.ManualUntil))
                throw new SmartHomeException("runtime_revision_conflict", 409);
        }
        if (request.Results == null || request.Results.Count > 1 || request.Results.Any(x => x == null || x.CommandId == Guid.Empty ||
            (x.Status != "confirmed" && x.Status != "failed" && x.Status != "unknown") ||
            (x.AppliedVersion.HasValue && !ValidVersion(x.AppliedVersion.Value)) ||
            (x.RuntimeRevision.HasValue && !ValidVersion(x.RuntimeRevision.Value)) ||
            (x.ErrorCode != null && !Identifier(x.ErrorCode))))
            throw new SmartHomeException("invalid_result");
    }

    // Same wall-clock interval rules as the local scheduler; the cloud validates but never evaluates or runs schedules.
    public static void ValidateSchedule(ScheduleConfigurationDto config, string deviceId)
    {
        if (config == null || !ValidVersion(config.Version) || config.DeviceId != deviceId || !Identifier(deviceId) ||
            config.TimeZone == null || config.TimeZone.Length > 100 || DateTimeZoneProviders.Tzdb.GetZoneOrNull(config.TimeZone) == null ||
            config.Intervals == null || config.Intervals.Count > 64)
            throw new SmartHomeException("invalid_schedule");
        var ids = new HashSet<Guid>();
        var periods = new List<(int start, int end, bool on)>();
        foreach (var interval in config.Intervals)
        {
            if (interval == null || interval.Id == Guid.Empty || !ids.Add(interval.Id) || interval.Name == null ||
                interval.Name.Length > 80 || interval.Name.Any(char.IsControl) || interval.Days == null ||
                interval.Days.Length is < 1 or > 7 || interval.Days.Distinct().Count() != interval.Days.Length ||
                interval.Days.Any(x => x is < 1 or > 7)) throw new SmartHomeException("invalid_schedule");
            var start = ScheduleMinute(interval.Start, false);
            var end = ScheduleMinute(interval.End, true);
            if (start == end) throw new SmartHomeException("invalid_schedule");
            if (end < start) end += 1440;
            if (interval.Enabled)
                foreach (var day in interval.Days)
                    periods.Add(((day - 1) * 1440 + start, (day - 1) * 1440 + end, interval.On));
        }
        for (var i = 0; i < periods.Count; i++)
        for (var j = i + 1; j < periods.Count; j++)
        {
            if (periods[i].on == periods[j].on) continue;
            for (var shift = -10080; shift <= 10080; shift += 10080)
                if (periods[i].start < periods[j].end + shift && periods[j].start + shift < periods[i].end)
                    throw new SmartHomeException("conflicting_schedule_intervals");
        }
    }

    private static int ScheduleMinute(string value, bool end)
    {
        if (value == null || value.Length != 5 || value[2] != ':' ||
            value.Where((_, i) => i != 2).Any(c => c < '0' || c > '9')) throw new SmartHomeException("invalid_schedule");
        var h = (value[0] - '0') * 10 + value[1] - '0';
        var m = (value[3] - '0') * 10 + value[4] - '0';
        if (m > 59 || (h > 23 && !(end && h == 24 && m == 0))) throw new SmartHomeException("invalid_schedule");
        return h * 60 + m;
    }

    private static bool ValidVersion(long version) => version >= 0 && version <= MaximumScheduleVersion;
    private static List<ScheduleCache> Schedules(SmartHomeDb home) => JsonSerializer.Deserialize<List<ScheduleCache>>(home.SchedulesJson) ?? new();
    private static bool ScheduleFresh(SmartHomeDb home, ScheduleCache snapshot, DateTimeOffset now)
    {
        var device = Devices(home).SingleOrDefault(x => x.Id == snapshot.DeviceId);
        return Fresh(home, now) && snapshot.ConnectionId == home.ConnectionId && snapshot.SnapshotAt >= home.ConnectedAt &&
            snapshot.SnapshotAt >= now.AddSeconds(-30) && device != null && device.Online && device.Power.HasValue && device.UpdatedAt >= now.AddSeconds(-30) &&
            device.Capabilities.Contains("power.read") && device.Capabilities.Contains("power.set");
    }
    private static void InvalidateAbsentSchedules(SmartHomeDb home)
    {
        var registered = Devices(home).Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var cache = Schedules(home);
        foreach (var removed in cache.Where(x => !registered.Contains(x.DeviceId))) removed.ConnectionId = null;
        home.SchedulesJson = JsonSerializer.Serialize(cache);
    }
    private static List<ScheduleSnapshotDto> ScheduleViews(SmartHomeDb home, DateTimeOffset now) => Schedules(home)
        .Where(x => Devices(home).Any(d => d.Id == x.DeviceId)).Select(x => new ScheduleSnapshotDto
    {
        DeviceId = x.DeviceId, SnapshotAt = x.SnapshotAt, Stale = !ScheduleFresh(home, x, now), Configuration = x.Configuration, Runtime = x.Runtime
    }).ToList();
    private static ScheduleConfigurationDto CanonicalConfiguration(ScheduleConfigurationDto config, bool ignoreVersion = false) => new()
    {
        Version = ignoreVersion ? 0 : config.Version, DeviceId = config.DeviceId, Enabled = config.Enabled,
        TimeZone = config.TimeZone, OutsidePower = config.OutsidePower,
        Intervals = config.Intervals.OrderBy(x => x.Id).Select(x => new ScheduleIntervalDto
        {
            Id = x.Id, Name = x.Name, Enabled = x.Enabled, Days = x.Days.OrderBy(d => d).ToArray(), Start = x.Start, End = x.End, On = x.On
        }).ToList()
    };
    private static bool SameConfiguration(ScheduleConfigurationDto a, ScheduleConfigurationDto b, bool ignoreVersion = false) =>
        a == null || b == null ? a == b : JsonSerializer.Serialize(CanonicalConfiguration(a, ignoreVersion)) == JsonSerializer.Serialize(CanonicalConfiguration(b, ignoreVersion));
    private static bool SameIntent(ScheduleCommandPayload a, ScheduleCommandPayload b) =>
        a != null && b != null && a.ExpectedVersion == b.ExpectedVersion && a.ExpectedRuntimeRevision == b.ExpectedRuntimeRevision &&
        SameConfiguration(a.Configuration, b.Configuration);
}
