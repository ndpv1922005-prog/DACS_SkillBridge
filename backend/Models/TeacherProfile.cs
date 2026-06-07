namespace SkillBridge.Api.Models;

public enum TeachingMode
{
    Online,
    Offline,
    Hybrid
}

public enum TeacherProfileStatus
{
    Draft,
    Pending,
    Active
}

public sealed record TeacherProfile(
    Guid Id,
    Guid UserId,
    string Skill,
    string Description,
    string Experience,
    decimal PricePerSession,
    TeachingMode TeachingMode,
    string OfflineLocation,
    string PortfolioImageUrl,
    TeacherProfileStatus Status,
    decimal Rating,
    bool IsOnline,
    string DefaultPayoutMethod = "Tài khoản ngân hàng",
    string DefaultPayoutBank = "Vietcombank");
