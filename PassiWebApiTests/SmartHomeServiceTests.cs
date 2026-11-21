using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NUnit.Framework;
using passi_webapi.SmartHome;
using Repos;
using WebApiDto.SmartHome;

namespace PassiWebApiTests;

[TestFixture, NonParallelizable]
public class SmartHomeServiceTests : TestBase
{
    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(int seconds) => Now = Now.AddSeconds(seconds);
    }
    private ManualClock clock;
    private Guid owner;
    private string thumbprint;

    private async Task<T> Use<T>(Func<SmartHomeService, Task<T>> action)
    {
        using var scope = ServiceProvider.CreateScope();
        return await action(new SmartHomeService(scope.ServiceProvider.GetRequiredService<PassiDbContext>(), clock));
    }
    private async Task Use(Func<SmartHomeService, Task> action)
    {
        using var scope = ServiceProvider.CreateScope();
        await action(new SmartHomeService(scope.ServiceProvider.GetRequiredService<PassiDbContext>(), clock));
    }

    [SetUp]
    public async Task ResetSmartHome()
    {
        clock = new ManualClock();
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
        await db.SmartHomeCommands.ExecuteDeleteAsync();
        await db.SmartHomes.ExecuteDeleteAsync();
        await db.SmartHomeOwners.ExecuteDeleteAsync();
        var user = await db.Users.Include(x => x.Certificates).Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.EmailHash == "admin@passi.cloud");
        if (user == null)
        {
            user = new UserDb { Guid = Guid.NewGuid(), EmailHash = "admin@passi.cloud" };
            user.Certificates.Add(new CertificateDb { Thumbprint = Guid.NewGuid().ToString("N"), PublicCert = "emulator" });
            user.Invitations.Add(new UserInvitationDb { Code = "123456", IsConfirmed = true });
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        owner = user.Guid;
        thumbprint = user.Certificates.First().Thumbprint;
        Assert.That(await Use(s => s.ResolveOwnerAsync("admin@passi.cloud", thumbprint)), Is.EqualTo(owner));
    }

    private ExchangeRequest Snapshot(bool power = false) => new()
    {
        SnapshotAt = clock.Now,
        Devices = new()
        {
            new DeviceDto
            {
                Id = "xiaomi-plug", Name = "Emulated plug", Model = "emulator", Online = true,
                Capabilities = new[] { "power.read", "temperature.read", "power.set" },
                Power = power, TemperatureCelsius = 32, UpdatedAt = clock.Now
            }
        }
    };

    private async Task<(Guid home, AgentIdentity identity, string token)> OnlineHome(string connection = "connection-a")
    {
        var registration = await Use(s => s.CreateHomeAsync(owner, "Emulated home"));
        var token = await Use(s => s.EnrollAsync(registration.HomeId, registration.EnrollmentSecret));
        var identity = await Use(s => s.AuthenticateAgentAsync(token.Token));
        await Use(s => s.ConnectAsync(identity, connection));
        await Use(s => s.ExchangeAsync(identity, connection, Snapshot()));
        return (registration.HomeId, identity, token.Token);
    }
    private Task<CommandDto> Queue(Guid home, Guid? id = null, bool on = true) => Use(s =>
        s.QueueCommandAsync(owner, home, "xiaomi-plug", new PowerCommandRequest { CommandId = id ?? Guid.NewGuid(), On = on }));
    private Task<DashboardDto> Dashboard() => Use(s => s.GetDashboardAsync(owner));
    private static async Task Reject(Func<Task> action, string code)
    {
        var error = await Assert.ThrowsAsync<SmartHomeException>(async () => await action());
        Assert.That(error.Code, Is.EqualTo(code));
    }

    [Test]
    public async Task OwnerUsesExistingGuidAndCertificate_NotClientEmailOrForeignGuid()
    {
        Assert.That(await Use(s => s.ResolveOwnerAsync("someone@passi.cloud", thumbprint)), Is.Null);
        Assert.That(await Use(s => s.ResolveOwnerAsync("admin@passi.cloud", "wrong")), Is.Null);
        await Reject(() => Use(s => s.GetDashboardAsync(Guid.NewGuid())), "forbidden");
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
        var user = await db.Users.SingleAsync(x => x.Guid == owner);
        var replacementGuid = Guid.NewGuid();
        user.Guid = replacementGuid;
        await db.SaveChangesAsync();
        try
        {
            Assert.That(await Use(s => s.ResolveOwnerAsync("admin@passi.cloud", thumbprint)), Is.Null);
            Assert.That((await db.SmartHomeOwners.SingleAsync()).OwnerId, Is.EqualTo(owner));
        }
        finally { user.Guid = owner; await db.SaveChangesAsync(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RetiredCertificateCannotAuthorizeOwnerOrCreateOwnerPin(bool removeExistingPin)
    {
        Assert.That(await Use(s => s.ResolveOwnerAsync("admin@passi.cloud", thumbprint)), Is.EqualTo(owner));
        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PassiDbContext>();
        var certificate = await db.Certificates.SingleAsync(x => x.Thumbprint == thumbprint);
        certificate.IsRetired = true;
        await db.SaveChangesAsync();
        if (removeExistingPin) await db.SmartHomeOwners.ExecuteDeleteAsync();
        try
        {
            Assert.That(await Use(s => s.ResolveOwnerAsync("admin@passi.cloud", thumbprint)), Is.Null);
            Assert.That(await db.SmartHomeOwners.CountAsync(), Is.EqualTo(removeExistingPin ? 0 : 1));
        }
        finally { certificate.IsRetired = false; await db.SaveChangesAsync(); }
    }

    [Test]
    public async Task EnrollmentIsOneUseBoundToHomeAndStoredHashed()
    {
        var enrollment = await Use(s => s.CreateHomeAsync(owner, "Home"));
        await Reject(() => Use(s => s.EnrollAsync(Guid.NewGuid(), enrollment.EnrollmentSecret)), "invalid_enrollment");
        var token = await Use(s => s.EnrollAsync(enrollment.HomeId, enrollment.EnrollmentSecret));
        await Reject(() => Use(s => s.EnrollAsync(enrollment.HomeId, enrollment.EnrollmentSecret)), "invalid_enrollment");
        Assert.That(await Use(s => s.AuthenticateAgentAsync("invalid-token")), Is.Null);
        using var scope = ServiceProvider.CreateScope();
        var home = await scope.ServiceProvider.GetRequiredService<PassiDbContext>().SmartHomes.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(home.EnrollmentHash, Is.Null);
            Assert.That(home.TokenHash, Has.Length.EqualTo(64));
            Assert.That(home.TokenHash, Is.Not.EqualTo(token.Token));
        });
    }

    [Test]
    public async Task ExpiredEnrollmentAndAgentTokenAreRejected()
    {
        var enrollment = await Use(s => s.CreateHomeAsync(owner, "Home"));
        clock.Advance(601);
        await Reject(() => Use(s => s.EnrollAsync(enrollment.HomeId, enrollment.EnrollmentSecret)), "invalid_enrollment");
        enrollment = await Use(s => s.ReenrollAsync(owner, enrollment.HomeId));
        var token = await Use(s => s.EnrollAsync(enrollment.HomeId, enrollment.EnrollmentSecret));
        clock.Now = token.ExpiresAt;
        Assert.That(await Use(s => s.AuthenticateAgentAsync(token.Token)), Is.Null);
    }

    [Test]
    public async Task RevocationAndRotationInvalidateLiveConnectionsAndOldCredentials()
    {
        var (home, identity, token) = await OnlineHome();
        var enrollment = await Use(s => s.ReenrollAsync(owner, home));
        Assert.That(await Use(s => s.AuthenticateAgentAsync(token)), Is.Null);
        await Reject(() => Use(s => s.ExchangeAsync(identity, "connection-a", Snapshot())), "invalid_agent");
        var newToken = await Use(s => s.EnrollAsync(home, enrollment.EnrollmentSecret));
        Assert.That(await Use(s => s.AuthenticateAgentAsync(newToken.Token)), Is.Not.Null);
        await Use(s => s.RevokeAsync(owner, home));
        Assert.That(await Use(s => s.AuthenticateAgentAsync(newToken.Token)), Is.Null);
    }

    [Test]
    public async Task AnotherHomeCannotClaimOrAcknowledgeCommands()
    {
        var first = await OnlineHome("first");
        var second = await OnlineHome("second");
        var command = await Queue(first.home);
        Assert.That((await Use(s => s.ExchangeAsync(second.identity, "second", Snapshot()))).Commands, Is.Empty);
        var report = Snapshot(true);
        report.Results.Add(new CommandResultDto { CommandId = command.Id, Status = "confirmed", Power = true, ObservedAt = clock.Now });
        await Reject(() => Use(s => s.ExchangeAsync(second.identity, "second", report)), "unknown_command");
        var forged = new AgentIdentity { HomeId = first.home, OwnerId = owner, TokenHash = second.identity.TokenHash, Generation = second.identity.Generation };
        await Reject(() => Use(s => s.ConnectAsync(forged, "forged")), "invalid_agent");
    }

    [Test]
    public async Task CommandDeduplicatesAcrossContextsAndConfirmsOnlyAfterReadback()
    {
        var home = await OnlineHome();
        var id = Guid.NewGuid();
        var command = await Queue(home.home, id);
        Assert.That((await Queue(home.home, id)).Id, Is.EqualTo(command.Id));
        await Reject(() => Queue(home.home, id, false), "command_id_conflict");
        var sent = await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()));
        Assert.That(sent.Commands.Single().Status, Is.EqualTo("sent"));
        Assert.That((await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()))).Commands, Is.Empty);
        clock.Advance(1);
        var report = Snapshot(true);
        report.Results.Add(new CommandResultDto { CommandId = id, Status = "confirmed", Power = true, ObservedAt = clock.Now });
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", report));
        Assert.That((await Dashboard()).Homes.Single().Commands.Single().Status, Is.EqualTo("confirmed"));
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", report));
        Assert.That((await Queue(home.home, id)).Status, Is.EqualTo("confirmed"));
    }

    [TestCase(false, "unknown")]
    [TestCase(true, "failed")]
    public async Task MissingOrMismatchedReadbackIsNeverConfirmed(bool includeReadback, string expected)
    {
        var home = await OnlineHome();
        var command = await Queue(home.home);
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()));
        var report = Snapshot(false);
        report.Results.Add(new CommandResultDto
        {
            CommandId = command.Id, Status = "confirmed", Power = includeReadback ? false : null,
            ObservedAt = includeReadback ? clock.Now : null
        });
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", report));
        Assert.That((await Dashboard()).Homes.Single().Commands.Single().Status, Is.EqualTo(expected));
    }

    [Test]
    public async Task DisconnectMakesDeliveredCommandUnknown_AndReconnectNeverRedelivers()
    {
        var home = await OnlineHome();
        await Queue(home.home);
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()));
        await Use(s => s.DisconnectAsync(home.identity, "connection-a"));
        var disconnected = (await Dashboard()).Homes.Single();
        Assert.Multiple(() =>
        {
            Assert.That(disconnected.Online, Is.False);
            Assert.That(disconnected.Stale, Is.True);
            Assert.That(disconnected.Commands.Single().Status, Is.EqualTo("unknown"));
            Assert.That(disconnected.Devices.Single().UpdatedAt, Is.EqualTo(clock.Now));
        });
        clock.Advance(1);
        await Use(s => s.ConnectAsync(home.identity, "connection-b"));
        await Reject(() => Queue(home.home), "home_offline_or_stale");
        await Use(s => s.DisconnectAsync(home.identity, "connection-a"));
        Assert.That((await Dashboard()).Homes.Single().Online, Is.True);
        Assert.That((await Use(s => s.ExchangeAsync(home.identity, "connection-b", Snapshot()))).Commands, Is.Empty);
        await Reject(() => Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot())), "connection_replaced");
    }

    [TestCase(false, "expired")]
    [TestCase(true, "unknown")]
    public async Task TimeoutDoesNotReplayCommands(bool delivered, string expected)
    {
        var home = await OnlineHome();
        var command = await Queue(home.home);
        if (delivered) await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()));
        clock.Advance(31);
        Assert.That((await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()))).Commands, Is.Empty);
        Assert.That((await Queue(home.home, command.Id)).Status, Is.EqualTo(expected));
    }

    [Test]
    public async Task StaleSnapshotsOfflineDevicesAndUnsupportedOperationsCannotBeControlled()
    {
        var home = await OnlineHome();
        var snapshot = Snapshot();
        clock.Advance(31);
        await Reject(() => Use(s => s.ExchangeAsync(home.identity, "connection-a", snapshot)), "invalid_or_stale_snapshot");
        var stale = (await Dashboard()).Homes.Single();
        Assert.That(stale.Stale, Is.True);
        await Reject(() => Queue(home.home), "home_offline_or_stale");
        snapshot = Snapshot();
        snapshot.Devices.Single().Online = false;
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", snapshot));
        await Reject(() => Queue(home.home), "device_offline_or_stale");
        snapshot.Devices.Single().Online = true;
        snapshot.Devices.Single().Capabilities = new[] { "power.read", "temperature.read" };
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", snapshot));
        await Reject(() => Queue(home.home), "unsupported_device_operation");
    }

    [Test]
    public async Task ConcurrentExchangesAcrossContextsClaimCommandOnce()
    {
        var home = await OnlineHome();
        var command = await Queue(home.home);
        var responses = await Task.WhenAll(
            Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot())),
            Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot())));
        Assert.That(responses.SelectMany(x => x.Commands).Count(x => x.Id == command.Id), Is.EqualTo(1));
        Assert.That((await Dashboard()).Homes.Single().Commands.Single().Status, Is.EqualTo("sent"));
    }

    [Test]
    public async Task LateSuccessCannotRewriteUnknownOutcome()
    {
        var home = await OnlineHome();
        var command = await Queue(home.home);
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()));
        clock.Advance(31);
        var report = Snapshot(true);
        report.Results.Add(new CommandResultDto { CommandId = command.Id, Status = "confirmed", Power = true, ObservedAt = clock.Now });
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", report));
        Assert.That((await Dashboard()).Homes.Single().Commands.Single().Status, Is.EqualTo("unknown"));
    }

    [Test]
    public async Task ReadbackBeforeDispatchCannotConfirmExecution()
    {
        var home = await OnlineHome();
        var command = await Queue(home.home);
        var observationBeforeDispatch = clock.Now;
        clock.Advance(5);
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", Snapshot()));
        var report = Snapshot(true);
        report.Results.Add(new CommandResultDto
        {
            CommandId = command.Id, Status = "confirmed", Power = true, ObservedAt = observationBeforeDispatch
        });
        await Use(s => s.ExchangeAsync(home.identity, "connection-a", report));
        Assert.That((await Dashboard()).Homes.Single().Commands.Single().Status, Is.EqualTo("unknown"));
    }

    [Test]
    public async Task NewSnapshotEnvelopeCannotRegressDeviceReadingTimestamp()
    {
        var home = await OnlineHome();
        clock.Advance(5);
        var report = Snapshot(true);
        report.Devices.Single().UpdatedAt = clock.Now.AddSeconds(-6);
        await Reject(() => Use(s => s.ExchangeAsync(home.identity, "connection-a", report)), "invalid_device");
        var view = (await Dashboard()).Homes.Single();
        Assert.That(view.Devices.Single().Power, Is.False);
    }
}
