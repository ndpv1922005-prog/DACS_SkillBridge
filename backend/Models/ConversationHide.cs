namespace SkillBridge.Api.Models;

public sealed record ConversationHide(
    Guid Id,
    Guid UserId,
    Guid ContactUserId,
    DateTimeOffset HiddenAt);
