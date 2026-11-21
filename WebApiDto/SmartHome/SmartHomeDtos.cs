using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebApiDto.SmartHome;

public class DashboardDto
{
    public DateTimeOffset ServerTime { get; set; }
    public List<HomeDto> Homes { get; set; } = new();
}
public class HomeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool Online { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? SnapshotAt { get; set; }
    public bool Stale { get; set; }
    public List<DeviceDto> Devices { get; set; } = new();
    public List<CommandDto> Commands { get; set; } = new();
}
public class DeviceDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Model { get; set; }
    public bool Online { get; set; }
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public bool? Power { get; set; }
    public double? TemperatureCelsius { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
public class EnrollmentDto
{
    public Guid HomeId { get; set; }
    public string EnrollmentSecret { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
public class AgentTokenDto
{
    public Guid HomeId { get; set; }
    public string Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
public class PowerCommandRequest
{
    public Guid CommandId { get; set; }
    [JsonRequired]
    public bool On { get; set; }
}
public class ExchangeRequest
{
    public DateTimeOffset SnapshotAt { get; set; }
    public List<DeviceDto> Devices { get; set; } = new();
    public List<CommandResultDto> Results { get; set; } = new();
}
public class CommandResultDto
{
    public Guid CommandId { get; set; }
    public string Status { get; set; }
    public bool? Power { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
    public string ErrorCode { get; set; }
}
public class ExchangeResponse
{
    public DateTimeOffset ServerTime { get; set; }
    public List<CommandDto> Commands { get; set; } = new();
}
public class CommandDto
{
    public Guid Id { get; set; }
    public Guid HomeId { get; set; }
    public string DeviceId { get; set; }
    public string Operation { get; set; } = "power.set";
    public bool On { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string ErrorCode { get; set; }
}
