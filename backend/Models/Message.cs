namespace SkillBridge.Api.Models;

public sealed record Message(
    Guid Id,
    Guid SenderId,
    Guid ReceiverId,
    string Content,
    DateTimeOffset CreatedAt,
    bool IsRead,
    bool IsDeleted);
