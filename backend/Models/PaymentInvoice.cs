namespace SkillBridge.Api.Models;

public enum InvoiceStatus
{
    Held,
    Completed,
    Refunded
}

public sealed record PaymentInvoice(
    Guid Id,
    string InvoiceCode,
    Guid BookingId,
    Guid StudentId,
    Guid TeacherId,
    string LessonContent,
    TeachingMode TeachingMode,
    decimal Amount,
    string PaymentMethod,
    InvoiceStatus Status,
    DateTimeOffset PaidAt,
    DateTimeOffset? UpdatedAt = null);
