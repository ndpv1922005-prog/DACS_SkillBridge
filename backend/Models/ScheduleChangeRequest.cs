namespace SkillBridge.Api.Models;

public enum ScheduleChangeType
{
    Reschedule,
    Cancel
}

public enum ScheduleChangeStatus
{
    Pending,
    Accepted,
    Rejected
}

public sealed record ScheduleChangeRequest(
    Guid Id,
    Guid BookingId,
    Guid StudentId,
    Guid TeacherId,
    DateTimeOffset CurrentStartTime,
    DateTimeOffset? RequestedStartTime,
    ScheduleChangeType Type,
    string Reason,
    ScheduleChangeStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);
