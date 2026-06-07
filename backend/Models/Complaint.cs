namespace SkillBridge.Api.Models;

public enum ComplaintStatus
{
    WaitingTeacherResponse,
    InReview,
    Refunded,
    ReleasedToTeacher,
    Closed
}

public sealed record Complaint(
    Guid Id,
    Guid BookingId,
    Guid StudentId,
    Guid TeacherId,
    string Reason,
    string StudentEvidenceUrl,
    string TeacherResponse,
    string TeacherEvidenceUrl,
    ComplaintStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? TeacherRespondedAt = null,
    DateTimeOffset? ResolvedAt = null);
