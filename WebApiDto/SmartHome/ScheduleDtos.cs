using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebApiDto.SmartHome;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ScheduleConfigurationDto
{
    [JsonRequired] public long Version { get; set; }
    [JsonRequired] public string DeviceId { get; set; }
    [JsonRequired] public bool Enabled { get; set; }
    [JsonRequired] public string TimeZone { get; set; }
    [JsonRequired] public bool? OutsidePower { get; set; }
    [JsonRequired] public List<ScheduleIntervalDto> Intervals { get; set; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ScheduleIntervalDto
{
    [JsonRequired] public Guid Id { get; set; }
    [JsonRequired] public string Name { get; set; }
    [JsonRequired] public bool Enabled { get; set; }
    [JsonRequired] public int[] Days { get; set; }
    [JsonRequired] public string Start { get; set; }
    [JsonRequired] public string End { get; set; }
    [JsonRequired] public bool On { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ScheduleRuntimeDto
{
    [JsonRequired] public long Revision { get; set; }
    [JsonRequired] public string State { get; set; }
    public bool? DesiredPower { get; set; }
    public bool? ActualPower { get; set; }
    public DateTimeOffset? CheckedAt { get; set; }
    public DateTimeOffset? NextCheckAt { get; set; }
    public DateTimeOffset? NextBoundary { get; set; }
    [JsonRequired] public bool ManualActive { get; set; }
    public DateTimeOffset? ManualUntil { get; set; }
    public string Error { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ScheduleSnapshotDto
{
    public string DeviceId { get; set; }
    public DateTimeOffset SnapshotAt { get; set; }
    public bool Stale { get; set; }
    public ScheduleConfigurationDto Configuration { get; set; }
    public ScheduleRuntimeDto Runtime { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ReplaceScheduleRequest
{
    [JsonRequired] public Guid CommandId { get; set; }
    [JsonRequired] public long ExpectedVersion { get; set; }
    [JsonRequired] public ScheduleConfigurationDto Configuration { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ResumeScheduleRequest
{
    [JsonRequired] public Guid CommandId { get; set; }
    [JsonRequired] public long ExpectedVersion { get; set; }
    [JsonRequired] public long ExpectedRuntimeRevision { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ExchangeScheduleRequest
{
    [JsonRequired] public int ProtocolVersion { get; set; }
    [JsonRequired] public string DeviceId { get; set; }
    [JsonRequired] public DateTimeOffset SnapshotAt { get; set; }
    [JsonRequired] public ScheduleConfigurationDto Configuration { get; set; }
    [JsonRequired] public ScheduleRuntimeDto Runtime { get; set; }
    public List<ScheduleCommandResultDto> Results { get; set; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ScheduleCommandResultDto
{
    [JsonRequired] public Guid CommandId { get; set; }
    [JsonRequired] public string Status { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
    public long? AppliedVersion { get; set; }
    public long? RuntimeRevision { get; set; }
    public string ErrorCode { get; set; }
}
