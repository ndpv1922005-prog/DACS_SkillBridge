namespace SkillBridge.Api.Models;

public enum TransactionType
{
    Hold,
    Release,
    Refund
}

public enum TransactionStatus
{
    Pending,
    Held,
    UnderReview,
    Released,
    Refunded
}

public sealed record Transaction(
    Guid Id,
    Guid BookingId,
    decimal Amount,
    TransactionType Type,
    TransactionStatus Status,
    DateTimeOffset CreatedAt);
