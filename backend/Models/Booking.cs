namespace SkillBridge.Api.Models;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Paid,
    InProgress,
    Completed,
    Cancelled,
    Rejected,
    Refunded
}

public sealed record Booking(
    Guid Id,
    Guid StudentId,
    Guid TeacherId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingStatus Status,
    string MeetingRoomId,
    TeachingMode TeachingMode = TeachingMode.Online,
    string OfflineLocation = "",
    string LessonContent = "",
    bool StudentCompleted = false,
    bool TeacherCompleted = false);
