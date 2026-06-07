namespace SkillBridge.Api.Models;

public sealed record StudentProfile(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Email,
    string Gender,
    DateTime? DateOfBirth,
    string Phone,
    string LearningGoal,
    string Bio,
    string AvatarUrl,
    DateTimeOffset UpdatedAt);
