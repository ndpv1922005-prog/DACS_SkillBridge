namespace SkillBridge.Api.Models;

public enum WithdrawalStatus
{
    Pending,
    Paid
}

public sealed record Withdrawal(
    Guid Id,
    Guid TeacherId,
    decimal Amount,
    WithdrawalStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    string Method = "Tài khoản ngân hàng",
    string AccountName = "",
    string AccountNumber = "",
    string BankName = "");
