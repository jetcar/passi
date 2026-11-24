using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;
using passi_webapi.SmartHome;
using WebApiDto.SmartHome;

namespace PassiWebApiTests;

public partial class SmartHomeTransportTests
{
    private ScheduleConfigurationDto ScheduleConfig(long version = 0, string device = "xiaomi-plug") => new()
    {
        Version = version, DeviceId = device, Enabled = false, TimeZone = "Europe/Tallinn", OutsidePower = false,
        Intervals = new() { new ScheduleIntervalDto { Id = Guid.Parse("fba85d1a-66a4-4919-9823-41682fd5d90e"),
            Name = "Emulator only", Enabled = true, Days = new[] { 1, 2, 3, 4, 5 }, Start = "06:00", End = "09:00", On = true } }
    };
    private ExchangeScheduleRequest ScheduleSnapshot(long version = 0, long revision = 0, bool manual = false) => new()
    {
        ProtocolVersion = 1, DeviceId = "xiaomi-plug", SnapshotAt = clock.Now, Configuration = ScheduleConfig(version),
        Runtime = new ScheduleRuntimeDto { Revision = revision, State = manual ? "manual" : "disabled", ManualActive = manual,
            ActualPower = false, CheckedAt = clock.Now }, Results = new()
    };
    private async Task<(AgentTokenDto token, HubConnection agent)> ScheduledHome(bool manual = false)
    {
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot(manual: manual));
        return (token, agent);
    }
    private string SchedulePath(Guid home, string suffix = "") => Api + $"/homes/{home}/devices/xiaomi-plug/schedule{suffix}";
    private ReplaceScheduleRequest Replacement(Guid? id = null, long version = 0) => new()
    {
        CommandId = id ?? Guid.NewGuid(), ExpectedVersion = version, Configuration = ScheduleConfig(version)
    };
    private async Task<CommandDto> ReplaceSchedule(Guid home, ReplaceScheduleRequest request = null) =>
        await Read<CommandDto>(await Send(HttpMethod.Post, SchedulePath(home), Jwt(), request ?? Replacement()));
    private async Task<CommandDto> ResumeSchedule(Guid home, Guid? id = null, long version = 0, long revision = 0) =>
        await Read<CommandDto>(await Send(HttpMethod.Post, SchedulePath(home, "/resume"), Jwt(),
            new ResumeScheduleRequest { CommandId = id ?? Guid.NewGuid(), ExpectedVersion = version, ExpectedRuntimeRevision = revision }));
    private static async Task ExpectHubError(Func<Task> action, string error)
    {
        var exception = await Assert.ThrowsAsync<HubException>(async () => await action());
        Assert.That(exception.Message, Does.Contain(error));
    }

    [Test]
    public async Task ScheduleRoutesRequireOwnerAndAgentOptIn_AndMissingFieldsReject()
    {
        var token = await Enroll(await CreateHome());
        await Connect(token);
        var path = SchedulePath(token.HomeId);
        Assert.That((await Send(HttpMethod.Post, path, body: Replacement())).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Send(HttpMethod.Post, path, Jwt("other@example.test"), Replacement())).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await Send(HttpMethod.Post, path, token.Token, Replacement())).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await Send(HttpMethod.Post, path, Jwt(), Replacement())).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That((await Send(HttpMethod.Post, path, Jwt(), new { commandId = Guid.NewGuid(), configuration = ScheduleConfig() })).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(HttpMethod.Post, path + "/resume", Jwt(), new { commandId = Guid.NewGuid(), expectedVersion = 0 })).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ScheduleReplaceIsSentOnlyByNewExchange_AndConfirmedByExactSavedConfiguration()
    {
        var (token, agent) = await ScheduledHome();
        var change = Replacement();
        change.Configuration.Enabled = true;
        var command = await ReplaceSchedule(token.HomeId, change);
        Assert.That(command.Operation, Is.EqualTo("schedule.replace"));
        Assert.That(command.ExpectedVersion, Is.Zero);
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot())).Commands, Is.Empty);
        var delivery = await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot());
        Assert.That(delivery.Commands.Single().Id, Is.EqualTo(command.Id));
        Assert.That((await Home(token.HomeId)).Schedules.Single().Configuration.Enabled, Is.False);
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot())).Commands, Is.Empty);
        clock.Advance(1);
        var saved = ScheduleSnapshot(1);
        saved.Configuration.Enabled = true;
        saved.Results.Add(new ScheduleCommandResultDto { CommandId = command.Id, Status = "confirmed", ObservedAt = clock.Now,
            AppliedVersion = 1, RuntimeRevision = 0 });
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", saved);
        var home = await Home(token.HomeId);
        Assert.That(home.Commands.Single().Status, Is.EqualTo("confirmed"));
        Assert.That(home.Schedules.Single().Configuration.Version, Is.EqualTo(1));
        Assert.That(home.Devices.Single().Power, Is.False, "Schedule save is not a physical power confirmation");
        Assert.That((await ReplaceSchedule(token.HomeId, change)).Status, Is.EqualTo("confirmed"));
        change.Configuration.Enabled = false;
        Assert.That((await Send(HttpMethod.Post, SchedulePath(token.HomeId), Jwt(), change)).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task ScheduleAndPowerCannotAcknowledgeEachOtherOrShareCommandIds()
    {
        var (token, agent) = await ScheduledHome();
        var command = await ReplaceSchedule(token.HomeId);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot());
        var powerProof = Snapshot();
        powerProof.Results.Add(new CommandResultDto { CommandId = command.Id, Status = "confirmed", Power = false, ObservedAt = clock.Now });
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("Exchange", powerProof), "command_operation_mismatch");
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/devices/xiaomi-plug/power", Jwt(),
            new { commandId = command.Id, on = false })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        clock.Advance(31);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        var power = await Queue(token.HomeId);
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot())).Commands, Is.Empty);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        var proof = ScheduleSnapshot();
        proof.Results.Add(new ScheduleCommandResultDto { CommandId = power.Id, Status = "confirmed", ObservedAt = clock.Now,
            AppliedVersion = 0, RuntimeRevision = 0 });
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", proof), "command_operation_mismatch");
    }

    [Test]
    public async Task ScheduleCommandsAreIsolatedByHomeAndDevice_AndCurrentToken()
    {
        var first = await ScheduledHome();
        var second = await ScheduledHome();
        var command = await ReplaceSchedule(first.token.HomeId);
        Assert.That((await second.agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot())).Commands, Is.Empty);
        var proof = ScheduleSnapshot();
        proof.Results.Add(new ScheduleCommandResultDto { CommandId = command.Id, Status = "unknown" });
        await ExpectHubError(() => second.agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", proof), "unknown_command");
        var wrong = ScheduleSnapshot();
        wrong.DeviceId = "missing-device";
        wrong.Configuration.DeviceId = wrong.DeviceId;
        await ExpectHubError(() => first.agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", wrong), "unsupported_device_operation");
        await Send(HttpMethod.Post, Api + $"/homes/{first.token.HomeId}/revoke", Jwt());
        await Assert.CatchAsync<Exception>(() => first.agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot()));
    }

    [Test]
    public async Task ResumeChecksBothVersionsAndNeverReleasesNewerManualPauseOnRetry()
    {
        var (token, agent) = await ScheduledHome(manual: true);
        var command = await ResumeSchedule(token.HomeId);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot(manual: true));
        var resumed = ScheduleSnapshot(revision: 1);
        resumed.Results.Add(new ScheduleCommandResultDto { CommandId = command.Id, Status = "confirmed", ObservedAt = clock.Now,
            AppliedVersion = 0, RuntimeRevision = 1 });
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", resumed);
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("confirmed"));
        var newerManual = ScheduleSnapshot(revision: 2, manual: true);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", newerManual);
        Assert.That((await ResumeSchedule(token.HomeId, command.Id)).Status, Is.EqualTo("confirmed"));
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", newerManual)).Commands, Is.Empty);
        Assert.That((await Home(token.HomeId)).Schedules.Single().Runtime.ManualActive, Is.True);
        Assert.That((await Send(HttpMethod.Post, SchedulePath(token.HomeId, "/resume"), Jwt(),
            new ResumeScheduleRequest { CommandId = Guid.NewGuid(), ExpectedVersion = 0, ExpectedRuntimeRevision = 1 })).StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
    }

    [TestCase("version")]
    [TestCase("revision")]
    public async Task LocalChangesBeforeClaimFailPendingCommandWithoutDelivery(string change)
    {
        var (token, agent) = await ScheduledHome(manual: true);
        var command = await ResumeSchedule(token.HomeId);
        var newer = change == "version" ? ScheduleSnapshot(1, manual: true) : ScheduleSnapshot(revision: 1, manual: true);
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", newer)).Commands, Is.Empty);
        var failed = (await Home(token.HomeId)).Commands.Single();
        Assert.That(failed.Id, Is.EqualTo(command.Id));
        Assert.That(failed.Status, Is.EqualTo("failed"));
        Assert.That(failed.ErrorCode, Is.EqualTo(change == "version" ? "version_conflict" : "runtime_revision_conflict"));
    }

    [TestCase(false, "expired")]
    [TestCase(true, "unknown")]
    public async Task ScheduleTimeoutAndLateProofNeverReplayOrRewriteOutcome(bool deliver, string terminal)
    {
        var (token, agent) = await ScheduledHome();
        var request = Replacement();
        var command = await ReplaceSchedule(token.HomeId, request);
        if (deliver) await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot());
        clock.Advance(31);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        var late = ScheduleSnapshot(1);
        late.Results.Add(new ScheduleCommandResultDto { CommandId = command.Id, Status = "confirmed", ObservedAt = clock.Now,
            AppliedVersion = 1, RuntimeRevision = 0 });
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", late)).Commands, Is.Empty);
        Assert.That((await ReplaceSchedule(token.HomeId, request)).Status, Is.EqualTo(terminal));
    }

    [Test]
    public async Task ScheduleReconnectAndMissingInventoryRequireFreshDeviceDeclaration()
    {
        var (token, agent) = await ScheduledHome();
        var previousTime = (await Home(token.HomeId)).Schedules.Single().SnapshotAt;
        await agent.StopAsync();
        clock.Advance(1);
        var replacement = await Connect(token);
        var cached = (await Home(token.HomeId)).Schedules.Single();
        Assert.That(cached.Stale, Is.True);
        Assert.That(cached.SnapshotAt, Is.EqualTo(previousTime));
        Assert.That((await Send(HttpMethod.Post, SchedulePath(token.HomeId), Jwt(), Replacement())).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        await replacement.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot());
        Assert.That((await Home(token.HomeId)).Schedules.Single().Stale, Is.False);
        var inventory = Snapshot();
        inventory.Devices.Clear();
        await replacement.InvokeAsync<ExchangeResponse>("Exchange", inventory);
        Assert.That((await Home(token.HomeId)).Schedules, Is.Empty);
        await ExpectHubError(() => replacement.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot()), "unsupported_device_operation");
    }

    [Test]
    public async Task ScheduleExchangeDoesNotRefreshInventoryOrAllowStaleEnvelope()
    {
        var (token, agent) = await ScheduledHome();
        var stale = ScheduleSnapshot();
        clock.Advance(21);
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot()), "home_offline_or_stale");
        Assert.That((await Home(token.HomeId)).Schedules.Single().Stale, Is.True);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        clock.Advance(10);
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", stale), "invalid_or_stale_snapshot");
    }

    [TestCase("content", "schedule_version_content_conflict")]
    [TestCase("revision", "runtime_revision_conflict")]
    [TestCase("old", "stale_schedule_version")]
    [TestCase("protocol", "unsupported_schedule_protocol")]
    [TestCase("device", "invalid_schedule")]
    public async Task ScheduleSnapshotsRejectProtocolIdentityAndVersionRegressions(string mutation, string expected)
    {
        var (_, agent) = await ScheduledHome();
        var snapshot = ScheduleSnapshot(1, 1);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", snapshot);
        switch (mutation)
        {
            case "content": snapshot.Configuration.Enabled = true; break;
            case "revision": snapshot.Runtime.ManualActive = true; break;
            case "old": snapshot.Configuration.Version = 0; break;
            case "protocol": snapshot.ProtocolVersion = 2; break;
            case "device": snapshot.Configuration.DeviceId = "other"; break;
        }
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", snapshot), expected);
    }

    [TestCase("content")]
    [TestCase("observed-before")]
    [TestCase("observed-after")]
    [TestCase("result-version")]
    public async Task ScheduleConfirmationRequiresExactContentAndPostDispatchObservation(string mutation)
    {
        var (token, agent) = await ScheduledHome();
        var command = await ReplaceSchedule(token.HomeId);
        clock.Advance(1);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot());
        var saved = ScheduleSnapshot(1);
        var result = new ScheduleCommandResultDto { CommandId = command.Id, Status = "confirmed", ObservedAt = clock.Now,
            AppliedVersion = 1, RuntimeRevision = 0 };
        saved.Results.Add(result);
        switch (mutation)
        {
            case "content": saved.Configuration.Enabled = true; break;
            case "observed-before": result.ObservedAt = clock.Now.AddSeconds(-1); break;
            case "observed-after": result.ObservedAt = clock.Now.AddSeconds(1); break;
            case "result-version": result.AppliedVersion = 0; break;
        }
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", saved);
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("unknown"));
    }

    [Test]
    public async Task ScheduleAndPowerShareSingleInflightDeviceLimit()
    {
        var (token, agent) = await ScheduledHome(manual: true);
        await Queue(token.HomeId);
        Assert.That((await Send(HttpMethod.Post, SchedulePath(token.HomeId), Jwt(), Replacement())).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        clock.Advance(31);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot(manual: true));
        await ResumeSchedule(token.HomeId);
        Assert.That((await Send(HttpMethod.Post, Api + $"/homes/{token.HomeId}/devices/xiaomi-plug/power", Jwt(),
            new { commandId = Guid.NewGuid(), on = true })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task MaximumVersionIsReadableButCannotBeIncremented()
    {
        var (_, agent) = await ScheduledHome();
        var snapshot = ScheduleSnapshot(SmartHomeService.MaximumScheduleVersion, SmartHomeService.MaximumScheduleVersion, true);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", snapshot);
        var token = (await Read<DashboardDto>(await Send(HttpMethod.Get, Api, Jwt()))).Homes.Single();
        Assert.That((await Send(HttpMethod.Post, SchedulePath(token.Id), Jwt(), Replacement(version: SmartHomeService.MaximumScheduleVersion))).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Send(HttpMethod.Post, SchedulePath(token.Id, "/resume"), Jwt(), new ResumeScheduleRequest
        { CommandId = Guid.NewGuid(), ExpectedVersion = SmartHomeService.MaximumScheduleVersion, ExpectedRuntimeRevision = SmartHomeService.MaximumScheduleVersion })).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [TestCase("timezone")]
    [TestCase("windows-zone")]
    [TestCase("same-time")]
    [TestCase("24-start")]
    [TestCase("duplicate-day")]
    [TestCase("bad-day")]
    [TestCase("duplicate-id")]
    [TestCase("oversize")]
    [TestCase("overlap")]
    [TestCase("sunday-overlap")]
    public void ScheduleValidatorRejectsInvalidOrContradictoryConfiguration(string mutation)
    {
        var config = ScheduleConfig();
        var interval = config.Intervals.Single();
        switch (mutation)
        {
            case "timezone": config.TimeZone = "Invalid/Nowhere"; break;
            case "windows-zone": config.TimeZone = "FLE Standard Time"; break;
            case "same-time": interval.End = interval.Start; break;
            case "24-start": interval.Start = "24:00"; break;
            case "duplicate-day": interval.Days = new[] { 1, 1 }; break;
            case "bad-day": interval.Days = new[] { 0 }; break;
            case "duplicate-id": config.Intervals.Add(interval); break;
            case "oversize": interval.Name = new string('x', 81); break;
            case "overlap": config.Intervals.Add(new ScheduleIntervalDto { Id = Guid.NewGuid(), Name = "off", Enabled = true,
                Days = new[] { 1 }, Start = "08:00", End = "10:00", On = false }); break;
            case "sunday-overlap": interval.Days = new[] { 7 }; interval.Start = "22:00"; interval.End = "06:00";
                config.Intervals.Add(new ScheduleIntervalDto { Id = Guid.NewGuid(), Name = "off", Enabled = true,
                    Days = new[] { 1 }, Start = "05:00", End = "07:00", On = false }); break;
        }
        Assert.Throws<SmartHomeException>(() => SmartHomeService.ValidateSchedule(config, config.DeviceId));
    }

    [Test]
    public void ScheduleValidatorAllowsFullDaysOvernightAndSamePowerOverlap()
    {
        var config = ScheduleConfig();
        config.Intervals.Single().Start = "00:00";
        config.Intervals.Single().End = "24:00";
        config.Intervals.Add(new ScheduleIntervalDto { Id = Guid.NewGuid(), Name = "overnight", Enabled = true,
            Days = new[] { 7 }, Start = "22:00", End = "06:00", On = true });
        Assert.DoesNotThrow(() => SmartHomeService.ValidateSchedule(config, config.DeviceId));
    }

    [Test]
    public async Task MaximumScheduleFitsSignalRAndHttpWithoutTruncation()
    {
        var (token, agent) = await ScheduledHome();
        var request = Replacement();
        request.Configuration.Intervals = Enumerable.Range(0, 64).Select(_ => new ScheduleIntervalDto
        { Id = Guid.NewGuid(), Name = new string('界', 80), Enabled = true, Days = new[] { 1, 2, 3, 4, 5, 6, 7 },
            Start = "00:00", End = "24:00", On = true }).ToList();
        var command = await ReplaceSchedule(token.HomeId, request);
        var reply = await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot());
        Assert.That(reply.Commands.Single().Configuration.Intervals.Count, Is.EqualTo(64));
        var input = ScheduleSnapshot(1);
        input.Configuration = request.Configuration;
        input.Configuration.Version = 1;
        input.Results.Add(new ScheduleCommandResultDto { CommandId = command.Id, Status = "confirmed", ObservedAt = clock.Now,
            AppliedVersion = 1, RuntimeRevision = 0 });
        var envelope = new { type = 1, invocationId = "1", target = "ExchangeSchedule", arguments = new[] { input } };
        Assert.That(JsonSerializer.SerializeToUtf8Bytes(envelope).Length + 1, Is.LessThan(64 * 1024));
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", input);
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("confirmed"));
    }

    [TestCase("unknown")]
    [TestCase("enabled")]
    [TestCase("interval-on")]
    public async Task ScheduleHttpRejectsUnknownOrMissingOperationFields(string mutation)
    {
        var (token, _) = await ScheduledHome();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.SerializeToNode(Replacement(), options).AsObject();
        var configuration = request["configuration"].AsObject();
        if (mutation == "unknown") configuration["script"] = "not-an-operation";
        else if (mutation == "enabled") configuration.Remove("enabled");
        else configuration["intervals"][0].AsObject().Remove("on");
        Assert.That((await Send(HttpMethod.Post, SchedulePath(token.HomeId), Jwt(), request)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Home(token.HomeId)).Commands, Is.Empty);
    }

    [Test]
    public async Task ScheduleAgentRejectsUnknownFieldsAndMissingRuntimeRevision()
    {
        var (_, agent) = await ScheduledHome();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.SerializeToNode(ScheduleSnapshot(), options).AsObject();
        request["unexpected"] = true;
        await Assert.ThrowsAsync<HubException>(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", request));
        request.Remove("unexpected");
        request["runtime"].AsObject().Remove("revision");
        await Assert.ThrowsAsync<HubException>(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", request));
    }

    [Test]
    public async Task ScheduleResultForDifferentRegisteredDeviceIsRejected()
    {
        var (token, agent) = await ScheduledHome();
        var inventory = Snapshot();
        inventory.Devices.Add(new DeviceDto { Id = "second-plug", Name = "Second emulator", Online = true,
            Capabilities = new[] { "power.read", "power.set" }, Power = false, UpdatedAt = clock.Now });
        await agent.InvokeAsync<ExchangeResponse>("Exchange", inventory);
        var second = ScheduleSnapshot();
        second.DeviceId = "second-plug";
        second.Configuration.DeviceId = "second-plug";
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", second);
        var command = await ReplaceSchedule(token.HomeId);
        Assert.That((await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", second)).Commands, Is.Empty);
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot());
        second.Results.Add(new ScheduleCommandResultDto { CommandId = command.Id, Status = "unknown" });
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", second), "unknown_command");
        Assert.That((await Home(token.HomeId)).Commands.Single().Status, Is.EqualTo("sent"));
    }

    [Test]
    public async Task ScheduleRemovalAndReadditionPreserveVersionAndRevisionTombstones()
    {
        var (token, agent) = await ScheduledHome();
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot(5, 7));
        var anotherInventory = Snapshot();
        anotherInventory.Devices.Single().Id = "second-plug";
        await agent.InvokeAsync<ExchangeResponse>("Exchange", anotherInventory);
        var anotherSchedule = ScheduleSnapshot();
        anotherSchedule.DeviceId = "second-plug";
        anotherSchedule.Configuration.DeviceId = "second-plug";
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", anotherSchedule);
        await agent.InvokeAsync<ExchangeResponse>("Exchange", Snapshot());
        Assert.That((await Home(token.HomeId)).Schedules.Single().Stale, Is.True);
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot(4, 7)), "stale_schedule_version");
        await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot(5, 6)), "stale_schedule_version");
        await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", ScheduleSnapshot(5, 7));
        Assert.That((await Home(token.HomeId)).Schedules.Single().Stale, Is.False);
    }

    [Test]
    public async Task HistoricalScheduleCacheRejectsNew129thDeviceWithoutDiscardingVersions()
    {
        var token = await Enroll(await CreateHome());
        var agent = await Connect(token);
        for (var i = 0; i <= 128; i++)
        {
            var id = "emulated-plug-" + i;
            var inventory = Snapshot();
            inventory.Devices.Single().Id = id;
            await agent.InvokeAsync<ExchangeResponse>("Exchange", inventory);
            var schedule = ScheduleSnapshot();
            schedule.DeviceId = id;
            schedule.Configuration.DeviceId = id;
            if (i < 128) await agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", schedule);
            else await ExpectHubError(() => agent.InvokeAsync<ExchangeResponse>("ExchangeSchedule", schedule), "schedule_cache_limit");
        }
    }
}
