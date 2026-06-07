using SkillBridge.Api.Models;

namespace SkillBridge.Api.DTOs;

public sealed record AuthRequest(string Name, string Email, string Password, UserRole Role);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(Guid Id, string Name, string Email, UserRole Role, string AvatarUrl, string Bio, string Token);

public sealed record TeacherResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string AvatarUrl,
    string Bio,
    string Skill,
    string Description,
    string Experience,
    decimal PricePerSession,
    TeachingMode TeachingMode,
    string OfflineLocation,
    string PortfolioImageUrl,
    TeacherProfileStatus Status,
    decimal Rating,
    bool IsOnline);

public sealed record BookingRequest(Guid StudentId, Guid TeacherProfileId, DateTimeOffset StartTime, int DurationMinutes, TeachingMode TeachingMode = TeachingMode.Online);
public sealed record BookingDecisionRequest(Guid TeacherId);
public sealed record PaymentRequest(Guid StudentId, string Method = "Ví SkillBridge");
public sealed record CompleteBookingRequest(Guid StudentId);
public sealed record TeacherCompleteBookingRequest(Guid TeacherId);
public sealed record SendMessageRequest(Guid SenderId, Guid ReceiverId, string Content);
public sealed record DeleteMessageRequest(Guid UserId);
public sealed record HideConversationRequest(Guid UserId, Guid ContactUserId);
public sealed record MarkConversationReadRequest(Guid UserId, Guid ContactUserId);
public sealed record ReviewTeacherRequest(Guid StudentId, int Stars, string Comment);
public sealed record ComplaintRequest(Guid StudentId, string Reason, string EvidenceUrl);
public sealed record TeacherComplaintResponseRequest(Guid TeacherId, string Response, string EvidenceUrl);
