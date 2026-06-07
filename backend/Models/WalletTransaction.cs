namespace SkillBridge.Api.Models;

public enum WalletTransactionType
{
    TopUp,
    Payment,
    Refund,
    Withdrawal
}

public sealed record WalletTransaction(
    Guid Id,
    Guid UserId,
    WalletTransactionType Type,
    string Method,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? BookingId = null,
    Guid? WithdrawalId = null,
    string Note = "",
    string TransactionCode = "");
