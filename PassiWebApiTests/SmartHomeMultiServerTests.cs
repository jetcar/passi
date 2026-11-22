using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigurationManager;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models;
using NUnit.Framework;
using passi_webapi.SmartHome;
using Repos;
using WebApiDto.SmartHome;

namespace PassiWebApiTests;

// Real production service, EF queries, PostgreSQL transactions and advisory locks.
// Every test owns a fresh database in this fixture's container: no migrations,
// shared database resets, network device adapters or existing application data.
[TestFixture, NonParallelizable]
public class SmartHomeMultiServerTests
{
    private IContainer postgres;
    private readonly string postgresPassword = Guid.NewGuid().ToString("N");
    private AppSetting settings;
    private readonly ManualClock clock = new();
    private Guid owner;

    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(int seconds = 1) => Now = Now.AddSeconds(seconds);
    }

    private sealed record Home(Guid Id, AgentIdentity Identity, string Token, string Connection);

    [OneTimeSetUp]
    public async Task StartPostgres()
    {
        var builder = new ContainerBuilder().WithImage("postgres:15.5")
            .WithPortBinding(5432, true).WithEnvironment("POSTGRES_PASSWORD", postgresPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("database system is ready to accept connections"));
        var endpoint = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrEmpty(endpoint)) builder = builder.WithDockerEndpoint(endpoint);
        postgres = builder.Build();
        await postgres.StartAsync();
    }

    [SetUp]
    public async Task CreateIsolatedDatabase()
    {
        clock.Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["AppSetting:DbHost"] = postgres.Hostname,
            ["AppSetting:DbPort"] = postgres.GetMappedPublicPort(5432).ToString(),
            ["AppSetting:PassiDbName"] = "multi_server_" + Guid.NewGuid().ToString("N"),
            ["AppSetting:DbUser"] = "postgres",
            ["AppSetting:DbPassword"] = postgresPassword,
            ["AppSetting:DbSslMode"] = "Disable"
        }).Build();
        // Pin all settings to this isolated container, ignoring ambient production configuration.
        settings = new AppSetting(configuration) { PrefferAppsettingFile = true };
        using var db = Context();
        await db.Database.EnsureCreatedAsync();
        owner = Guid.NewGuid();
        db.Users.Add(new UserDb { Guid = owner, EmailHash = "synthetic-owner@example.test" });
        db.SmartHomeOwners.Add(new SmartHomeOwnerDb { Id = 1, OwnerId = owner });
        await db.SaveChangesAsync();
    }

    [OneTimeTearDown]
    public async Task StopPostgres()
    {
        if (postgres != null) await postgres.DisposeAsync();
    }

    private PassiDbContext Context() => new(settings, new CurrentContext());

    private async Task<T> Use<T>(Func<SmartHomeService, Task<T>> action)
    {
        using var db = Context();
        return await action(new SmartHomeService(db, clock));
    }

    private async Task Use(Func<SmartHomeService, Task> action)
    {
        using var db = Context();
        await action(new SmartHomeService(db, clock));
    }

    private DeviceDto Device(string id = "shared-plug", bool? power = false,
        string[] capabilities = null, string model = "synthetic.plug", double? temperature = null) => new()
    {
        Id = id, Name = "Synthetic " + id, Model = model, Online = true,
        Capabilities = capabilities ?? new[] { "power.read", "power.set" },
        Power = power, TemperatureCelsius = temperature, UpdatedAt = clock.Now
    };

    private ExchangeRequest Snapshot(params DeviceDto[] devices) => new()
    { SnapshotAt = clock.Now, Devices = devices.ToList() };

    private async Task<Home> OnlineHome(string name)
    {
        var registration = await Use(service => service.CreateHomeAsync(owner, name));
        var token = await Use(service => service.EnrollAsync(registration.HomeId, registration.EnrollmentSecret));
        var identity = await Use(service => service.AuthenticateAgentAsync(token.Token));
        var home = new Home(registration.HomeId, identity, token.Token, Guid.NewGuid().ToString("N"));
        await Use(service => service.ConnectAsync(home.Identity, home.Connection));
        await Exchange(home, Device());
        return home;
    }

    private Task<ExchangeResponse> Exchange(Home home, params DeviceDto[] devices) =>
        Use(service => service.ExchangeAsync(home.Identity, home.Connection, Snapshot(devices)));

    private Task<CommandDto> Queue(Home home, string deviceId = "shared-plug", bool on = true, Guid? commandId = null) =>
        Use(service => service.QueueCommandAsync(owner, home.Id, deviceId,
            new PowerCommandRequest { CommandId = commandId ?? Guid.NewGuid(), On = on }));

    private async Task<HomeDto> View(Home home) =>
        (await Use(service => service.GetDashboardAsync(owner))).Homes.Single(value => value.Id == home.Id);

    private static string Json(HomeDto home) => JsonSerializer.Serialize(home);

    private static async Task Reject(Func<Task> action, string code, int status = 400)
    {
        var error = await Assert.ThrowsAsync<SmartHomeException>(async () => await action());
        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.EqualTo(code));
            Assert.That(error.StatusCode, Is.EqualTo(status));
        });
    }

    private CommandResultDto Confirm(CommandDto command) => new()
    { CommandId = command.Id, Status = "confirmed", Power = command.On, ObservedAt = clock.Now };

    [Test]
    public async Task IdenticalDeviceIdsKeepIndependentSnapshotsBusyGatesAndCommandDelivery()
    {
        var first = await OnlineHome("First server");
        var second = await OnlineHome("Second server");
        var secondReading = Device(power: false, model: "synthetic.relay");
        clock.Advance(2);
        await Exchange(first, Device(power: true));
        // A timestamp older than the other server's reading is still valid for this server.
        await Exchange(second, secondReading);
        var firstView = await View(first);
        var secondView = await View(second);
        Assert.Multiple(() =>
        {
            Assert.That(firstView.Devices.Single().Power, Is.True);
            Assert.That(secondView.Devices.Single().Power, Is.False);
            Assert.That(firstView.Devices.Single().Model, Is.EqualTo("synthetic.plug"));
            Assert.That(secondView.Devices.Single().Model, Is.EqualTo("synthetic.relay"));
            Assert.That(secondView.Devices.Single().UpdatedAt, Is.LessThan(firstView.Devices.Single().UpdatedAt));
        });
        var commands = await Task.WhenAll(Queue(first, on: false), Queue(second, on: true));
        await Reject(() => Queue(first), "command_in_progress", 409);
        await Reject(() => Queue(second), "command_in_progress", 409);
        await Reject(() => Queue(second, on: false, commandId: commands[0].Id), "command_id_conflict", 409);
        var responses = await Task.WhenAll(Exchange(first, Device(power: true)), Exchange(second, Device()));
        for (var i = 0; i < responses.Length; i++)
        {
            var delivered = responses[i].Commands.Single();
            Assert.Multiple(() =>
            {
                Assert.That(delivered.Id, Is.EqualTo(commands[i].Id));
                Assert.That(delivered.HomeId, Is.EqualTo(i == 0 ? first.Id : second.Id));
                Assert.That(delivered.DeviceId, Is.EqualTo("shared-plug"));
                Assert.That(delivered.On, Is.EqualTo(i == 1));
                Assert.That(delivered.Status, Is.EqualTo("sent"));
            });
        }
        Assert.That((await Exchange(first, Device())).Commands, Is.Empty);
        Assert.That((await Exchange(second, Device())).Commands, Is.Empty);
        Assert.That((await View(first)).Commands.Select(command => command.Id), Is.EqualTo(new[] { commands[0].Id }));
        Assert.That((await View(second)).Commands.Select(command => command.Id), Is.EqualTo(new[] { commands[1].Id }));
    }

    [Test]
    public async Task ForeignResultRollsBackOwnResultAndSnapshot_ThenEachHomeConfirmsOnlyItsOwnCommand()
    {
        var first = await OnlineHome("First server");
        var second = await OnlineHome("Second server");
        var firstCommand = await Queue(first, on: true);
        var secondCommand = await Queue(second, on: false);
        await Exchange(first, Device());
        await Exchange(second, Device(power: true));
        var beforeFirst = Json(await View(first));
        var beforeSecond = Json(await View(second));
        clock.Advance();
        var mixed = Snapshot(Device(power: false));
        mixed.Results.Add(Confirm(secondCommand));
        mixed.Results.Add(Confirm(firstCommand));
        await Reject(() => Use(service => service.ExchangeAsync(second.Identity, second.Connection, mixed)), "unknown_command", 404);
        Assert.That(Json(await View(first)), Is.EqualTo(beforeFirst));
        Assert.That(Json(await View(second)), Is.EqualTo(beforeSecond),
            "Foreign result must roll back the preceding valid result, snapshot and heartbeat together.");

        var firstReport = Snapshot(Device(power: true));
        firstReport.Results.Add(Confirm(firstCommand));
        await Use(service => service.ExchangeAsync(first.Identity, first.Connection, firstReport));
        Assert.That((await View(first)).Commands.Single().Status, Is.EqualTo("confirmed"));
        Assert.That((await View(second)).Commands.Single().Status, Is.EqualTo("sent"));
        Assert.That((await View(second)).Devices.Single().Power, Is.True);
        var secondReport = Snapshot(Device(power: false));
        secondReport.Results.Add(Confirm(secondCommand));
        await Use(service => service.ExchangeAsync(second.Identity, second.Connection, secondReport));
        Assert.That((await View(second)).Commands.Single().Status, Is.EqualTo("confirmed"));
        Assert.That((await View(first)).Devices.Single().Power, Is.True);
        Assert.That((await View(second)).Devices.Single().Power, Is.False);
    }

    [Test]
    public async Task FullSnapshotRemovalExpiresOnlyRemovedDevicesQueuedWorkInItsOwnHome()
    {
        var first = await OnlineHome("First server");
        var second = await OnlineHome("Second server");
        await Exchange(first, Device(), Device("kept-plug"));
        var removedCommand = await Queue(first);
        var keptCommand = await Queue(first, "kept-plug");
        var secondCommand = await Queue(second);
        var beforeSecond = Json(await View(second));
        clock.Advance();
        var response = await Exchange(first, Device("kept-plug"));
        Assert.That(response.Commands.Select(command => command.Id), Is.EqualTo(new[] { keptCommand.Id }));
        var firstView = await View(first);
        Assert.That(firstView.Devices.Select(device => device.Id), Is.EqualTo(new[] { "kept-plug" }));
        Assert.That(firstView.Commands.Single(command => command.Id == removedCommand.Id).Status, Is.EqualTo("expired"));
        Assert.That(Json(await View(second)), Is.EqualTo(beforeSecond));
        await Reject(() => Queue(first), "unsupported_device_operation");
        Assert.That((await Exchange(second, Device())).Commands.Single().Id, Is.EqualTo(secondCommand.Id));

        await Exchange(first);
        Assert.That((await View(first)).Devices, Is.Empty);
        Assert.That((await View(second)).Devices.Single().Id, Is.EqualTo("shared-plug"));
        // Removing a sent device is not evidence that its command never executed.
        Assert.That((await View(first)).Commands.Single(command => command.Id == keptCommand.Id).Status, Is.EqualTo("sent"));
        await Exchange(first, Device());
        var replacement = await Queue(first);
        Assert.That(replacement.Id, Is.Not.EqualTo(removedCommand.Id));
        Assert.That((await View(second)).Commands.Single().Status, Is.EqualTo("sent"));
    }

    [Test]
    public async Task HeterogeneousModelsUseDeclaredCapabilities_AndDowngradesDoNotAffectAnotherHome()
    {
        var first = await OnlineHome("Mixed devices");
        var second = await OnlineHome("Independent relay");
        DeviceDto[] Devices() => new[]
        {
            Device(model: "synthetic.switch", capabilities: new[] { "power.read", "power.set", "temperature.read" }, temperature: 32),
            Device("relay", model: "synthetic.relay"),
            Device("thermometer", null, new[] { "temperature.read" }, "synthetic.sensor", 21.5),
            Device("read-only-plug", false, new[] { "power.read" }, "synthetic.monitor"),
            Device("undeclared", null, Array.Empty<string>(), "synthetic.unknown"),
            Device("set-without-read", null, new[] { "power.set" }, "synthetic.incomplete")
        };
        await Exchange(first, Devices());
        var view = await View(first);
        Assert.That(view.Devices.Count, Is.EqualTo(6));
        Assert.That(view.Devices.Select(device => device.Model).Distinct().Count(), Is.EqualTo(6));
        Assert.That(view.Devices.Single(device => device.Id == "thermometer").TemperatureCelsius, Is.EqualTo(21.5));
        foreach (var id in new[] { "thermometer", "read-only-plug", "undeclared", "set-without-read" })
            await Reject(() => Queue(first, id), "unsupported_device_operation");
        var plug = await Queue(first);
        var relay = await Queue(first, "relay");
        var other = await Queue(second);
        var beforeSecond = Json(await View(second));
        var downgraded = Devices();
        downgraded[0].Capabilities = new[] { "power.read", "temperature.read" };
        var delivered = await Exchange(first, downgraded);
        Assert.That(delivered.Commands.Select(command => command.Id), Is.EqualTo(new[] { relay.Id }));
        Assert.That((await View(first)).Commands.Single(command => command.Id == plug.Id).Status, Is.EqualTo("expired"));
        Assert.That(Json(await View(second)), Is.EqualTo(beforeSecond));
        Assert.That((await Exchange(second, Device())).Commands.Single().Id, Is.EqualTo(other.Id));
        var unsupported = Devices();
        unsupported[2].Capabilities = new[] { "humidity.read" };
        var beforeInvalid = Json(await View(first));
        await Reject(() => Exchange(first, unsupported), "invalid_device");
        Assert.That(Json(await View(first)), Is.EqualTo(beforeInvalid));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RevokingOrRotatingOneServerDoesNotChangeOtherServerCredentialsSnapshotsOrCommands(bool rotate)
    {
        var first = await OnlineHome("First server");
        var second = await OnlineHome("Second server");
        await Exchange(first, Device(), Device("queued-plug"));
        await Exchange(second, Device(), Device("queued-plug"));
        var firstSent = await Queue(first);
        var secondSent = await Queue(second);
        await Exchange(first, Device(), Device("queued-plug"));
        await Exchange(second, Device(), Device("queued-plug"));
        var firstQueued = await Queue(first, "queued-plug");
        var secondQueued = await Queue(second, "queued-plug");
        var beforeSecond = Json(await View(second));
        EnrollmentDto enrollment = null;
        if (rotate) enrollment = await Use(service => service.ReenrollAsync(owner, first.Id));
        else await Use(service => service.RevokeAsync(owner, first.Id));
        Assert.That(await Use(service => service.AuthenticateAgentAsync(first.Token)), Is.Null);
        var survivingIdentity = await Use(service => service.AuthenticateAgentAsync(second.Token));
        Assert.That(survivingIdentity.HomeId, Is.EqualTo(second.Id));
        Assert.That(survivingIdentity.Generation, Is.EqualTo(second.Identity.Generation));
        await Reject(() => Exchange(first, Device(power: true)), "invalid_agent", 401);
        await Use(service => service.DisconnectAsync(first.Identity, first.Connection));
        var firstView = await View(first);
        Assert.That(firstView.Commands.Single(command => command.Id == firstSent.Id).Status, Is.EqualTo("unknown"));
        Assert.That(firstView.Commands.Single(command => command.Id == firstQueued.Id).Status, Is.EqualTo("expired"));
        Assert.That(firstView.Online, Is.False);
        Assert.That(Json(await View(second)), Is.EqualTo(beforeSecond));
        if (rotate)
        {
            var replacement = await Use(service => service.EnrollAsync(first.Id, enrollment.EnrollmentSecret));
            var identity = await Use(service => service.AuthenticateAgentAsync(replacement.Token));
            await Use(service => service.ConnectAsync(identity, "replacement"));
            Assert.That((await Use(service => service.ExchangeAsync(identity, "replacement", Snapshot(Device())))).Commands, Is.Empty);
            Assert.That(Json(await View(second)), Is.EqualTo(beforeSecond));
        }
        clock.Advance();
        var report = Snapshot(Device(power: true), Device("queued-plug"));
        report.Results.Add(Confirm(secondSent));
        var response = await Use(service => service.ExchangeAsync(second.Identity, second.Connection, report));
        Assert.That(response.Commands.Single().Id, Is.EqualTo(secondQueued.Id));
        Assert.That((await View(second)).Commands.Single(command => command.Id == secondSent.Id).Status, Is.EqualTo("confirmed"));
    }

    [Test]
    public async Task ReconnectAndStaleDisconnectAffectOnlyTheOwningHome()
    {
        var first = await OnlineHome("First server");
        var second = await OnlineHome("Second server");
        await Queue(first);
        var secondCommand = await Queue(second);
        await Exchange(first, Device());
        await Exchange(second, Device());
        var beforeSecond = Json(await View(second));
        await Use(service => service.ConnectAsync(first.Identity, "first-reconnected"));
        await Use(service => service.DisconnectAsync(first.Identity, first.Connection));
        await Reject(() => Exchange(first, Device()), "connection_replaced", 409);
        await Reject(() => Use(service => service.ExchangeAsync(second.Identity, "first-reconnected", Snapshot(Device()))),
            "connection_replaced", 409);
        Assert.That(Json(await View(second)), Is.EqualTo(beforeSecond));
        Assert.That((await View(first)).Commands.Single().Status, Is.EqualTo("unknown"));
        Assert.That((await Use(service => service.ExchangeAsync(first.Identity, "first-reconnected", Snapshot(Device())))).Commands, Is.Empty);
        clock.Advance();
        var report = Snapshot(Device(power: true));
        report.Results.Add(Confirm(secondCommand));
        await Use(service => service.ExchangeAsync(second.Identity, second.Connection, report));
        Assert.That((await View(second)).Commands.Single().Status, Is.EqualTo("confirmed"));
    }

    [Test]
    public async Task ThirtyTwoDeviceLimitIsPerHomeAndRejectedSnapshotDoesNotReplaceEitherInventory()
    {
        var first = await OnlineHome("First server");
        var second = await OnlineHome("Second server");
        var devices = Enumerable.Range(1, 32).Select(index => Device("device-" + index)).ToArray();
        await Exchange(first, devices);
        await Exchange(second, devices.Select(device => Device(device.Id, power: true)).ToArray());
        var beforeFirst = Json(await View(first));
        var beforeSecond = Json(await View(second));
        Assert.That((await View(first)).Devices.Count, Is.EqualTo(32));
        Assert.That((await View(second)).Devices.Count, Is.EqualTo(32));
        await Reject(() => Exchange(first, devices.Append(Device("device-33")).ToArray()), "invalid_or_stale_snapshot");
        Assert.That(Json(await View(first)), Is.EqualTo(beforeFirst));
        Assert.That(Json(await View(second)), Is.EqualTo(beforeSecond));
        var firstCommand = await Queue(first, "device-32");
        var secondCommand = await Queue(second, "device-32", on: false);
        Assert.That((await Exchange(first, devices)).Commands.Single().Id, Is.EqualTo(firstCommand.Id));
        Assert.That((await Exchange(second, devices.Select(device => Device(device.Id, power: true)).ToArray())).Commands.Single().Id,
            Is.EqualTo(secondCommand.Id));
    }

    [Test]
    public async Task TenHomesAcceptIndependentIdenticalDeviceIds_AndEleventhDoesNotChangeExistingHomes()
    {
        var homes = new List<Home>();
        for (var index = 0; index < 10; index++) homes.Add(await OnlineHome("Server " + index));
        var before = await Use(service => service.GetDashboardAsync(owner));
        Assert.That(before.Homes.Count, Is.EqualTo(10));
        Assert.That(before.Homes.All(home => home.Devices.Single().Id == "shared-plug" && home.Online), Is.True);
        await Reject(() => Use(service => service.CreateHomeAsync(owner, "Eleventh")), "home_limit", 409);
        var after = await Use(service => service.GetDashboardAsync(owner));
        Assert.That(JsonSerializer.Serialize(after), Is.EqualTo(JsonSerializer.Serialize(before)));
        foreach (var home in homes)
        {
            var command = await Queue(home);
            var response = await Exchange(home, Device());
            Assert.That(response.Commands.Single().Id, Is.EqualTo(command.Id));
            Assert.That(response.Commands.Single().HomeId, Is.EqualTo(home.Id));
        }
    }
}
