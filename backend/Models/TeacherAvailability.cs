namespace SkillBridge.Api.Models;

public sealed record TeacherAvailability(
    Guid Id,
    Guid TeacherId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsActive,
    string PlannedContent,
    TeachingMode TeachingMode = TeachingMode.Online,
    string OfflineLocation = "");
