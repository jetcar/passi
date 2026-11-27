using System;

namespace Models;

// Deliberately no foreign key to Users: the owner pin is a tombstone after account deletion.
public class SmartHomeOwnerDb
{
    public int Id { get; set; }
    public Guid OwnerId { get; set; }
}

public class SmartHomeDb
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; }
    public string EnrollmentHash { get; set; }
    public DateTimeOffset? EnrollmentExpiresAt { get; set; }
    public string TokenHash { get; set; }
    public DateTimeOffset? TokenExpiresAt { get; set; }
    public long Generation { get; set; }
    public string ConnectionId { get; set; }
    public DateTimeOffset? ConnectedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? SnapshotAt { get; set; }
    public bool AwaitingSnapshot { get; set; }
    public string DevicesJson { get; set; } = "[]";
    public string SchedulesJson { get; set; } = "[]";
}

public class SmartHomeCommandDb
{
    public Guid Id { get; set; }
    public Guid HomeId { get; set; }
    public string DeviceId { get; set; }
    public bool On { get; set; }
    public string Operation { get; set; } = "power.set";
    public string PayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string Status { get; set; }
    public string ConnectionId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string ErrorCode { get; set; }
}
