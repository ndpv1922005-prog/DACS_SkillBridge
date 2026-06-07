namespace SkillBridge.Api.Models;

public sealed record Notification(
    Guid Id,
    Guid UserId,
    string Icon,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset CreatedAt);
