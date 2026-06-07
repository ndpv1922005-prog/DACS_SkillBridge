namespace SkillBridge.Api.Models;

public sealed record TeacherReview(
    Guid Id,
    Guid BookingId,
    Guid TeacherId,
    Guid StudentId,
    int Stars,
    string Comment,
    DateTimeOffset CreatedAt);
