namespace SkillBridge.Api.Models;

public enum UserRole
{
    Student,
    Teacher,
    Admin
}

public sealed record User(
    Guid Id,
    string Name,
    string Email,
    string PasswordHash,
    UserRole Role,
    string AvatarUrl,
    string Bio,
    DateTimeOffset CreatedAt,
    bool IsLocked = false);
