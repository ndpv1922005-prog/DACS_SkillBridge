using SkillBridge.Api.DTOs;
using SkillBridge.Api.Models;

namespace SkillBridge.Api.ViewModels;

public sealed record HomeViewModel(
    IEnumerable<TeacherResponse> Teachers,
    IEnumerable<Booking> Bookings,
    Guid? CurrentUserId,
    string? CurrentUserName);

public sealed record StudentDashboardViewModel(
    IEnumerable<TeacherResponse> Teachers,
    IEnumerable<Booking> Bookings,
    StudentProfileEditViewModel Profile,
    decimal WalletBalance,
    decimal TotalHeld,
    decimal TotalPaid,
    decimal TotalRefunded,
    IEnumerable<PaymentInvoice> Invoices,
    IEnumerable<WalletTransaction> WalletTransactions,
    string? Success = null,
    string? Error = null);

public sealed class StudentProfileEditViewModel
{
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Gender { get; set; } = "";
    public string BirthDate { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Bio { get; set; } = "";
    public string LearningGoal { get; set; } = "";
}

public sealed record LoginViewModel(string Email, string Password, string? Error = null);

public sealed record RegisterViewModel(
    string Name,
    string Email,
    string Password,
    UserRole Role,
    string? Error = null);

public sealed record ForgotPasswordViewModel(
    string Email,
    bool Submitted = false);

public sealed record ResetPasswordViewModel(
    string NewPassword,
    string ConfirmPassword,
    string? Message = null,
    string? Error = null);

public sealed record ChangePasswordViewModel(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword,
    string Language = "vi",
    string? Success = null,
    string? Error = null);

public sealed record TeacherListViewModel(IEnumerable<TeacherResponse> Teachers, string? Query, TeachingMode? Mode);

public sealed record BookingListViewModel(
    IEnumerable<Booking> Bookings,
    IEnumerable<TeacherResponse> Teachers,
    IEnumerable<ScheduleChangeRequest> ScheduleChangeRequests,
    Guid? CurrentUserId,
    IEnumerable<Guid>? UnderReviewBookingIds = null,
    IEnumerable<Guid>? ReviewedBookingIds = null);

public sealed record ChatViewModel(IEnumerable<ChatContactViewModel> Contacts, Guid CurrentUserId, Guid? InitialContactId = null);

public sealed record ChatContactViewModel(
    Guid UserId,
    string Name,
    string RoleLabel,
    string AvatarUrl,
    string LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount = 0,
    bool IsHidden = false);

public sealed record LayoutNotificationViewModel(
    Guid Id,
    string Icon,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    bool IsRead,
    bool IsDemo = false);

public sealed record TeacherDashboardViewModel(
    TeacherProfileEditViewModel Profile,
    bool HasProfile,
    int RegisteredStudents,
    int PendingBookings,
    int ConfirmedSessions,
    int CompletedSessions,
    TeacherProfessionalOverviewViewModel ProfessionalOverview,
    TeacherPaymentOverviewViewModel PaymentOverview,
    IEnumerable<TeacherBookingRowViewModel> Bookings,
    IEnumerable<TeacherStudentProgressViewModel> StudentProgress,
    IEnumerable<TeacherDayScheduleViewModel> DaySchedules,
    IEnumerable<TeacherAvailabilityDayViewModel> AvailabilityDays,
    string? Success = null,
    string? Error = null);

public sealed record TeacherProfessionalOverviewViewModel(
    decimal AverageRating,
    int TotalReviews,
    int CompletedSessions,
    int UniqueStudents,
    decimal SimulatedIncome,
    IEnumerable<TeacherReviewViewModel> RecentReviews);

public sealed record TeacherReviewViewModel(
    string StudentName,
    int Stars,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record TeacherPaymentOverviewViewModel(
    decimal TotalPaid,
    decimal TotalHeld,
    decimal TotalReleased,
    decimal PlatformCommission,
    decimal TeacherNetIncome,
    decimal TotalReceived,
    decimal WithdrawableBalance,
    decimal TotalWithdrawn,
    decimal PendingWithdrawal,
    IEnumerable<TeacherPaymentTransactionViewModel> Transactions,
    IEnumerable<TeacherWithdrawalViewModel> Withdrawals);

public sealed record TeacherPaymentTransactionViewModel(
    Guid Id,
    string StudentName,
    int SessionCount,
    decimal TotalAmount,
    decimal CommissionAmount,
    decimal TeacherNetAmount,
    string Status,
    DateTimeOffset CreatedAt,
    string Method = "Ví SkillBridge",
    string TransactionCode = "");

public sealed record TeacherWithdrawalViewModel(
    Guid Id,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt,
    string Method = "",
    string AccountName = "",
    string AccountNumber = "",
    string BankName = "");

public sealed class TeacherProfileEditViewModel
{
    public TeacherProfileEditViewModel()
    {
    }

    public TeacherProfileEditViewModel(
        Guid? id,
        string displayName,
        string avatarUrl,
        string skill,
        string description,
        string experience,
        decimal pricePerSession,
        TeachingMode teachingMode,
        string offlineLocation,
        string portfolioImageUrl,
        TeacherProfileStatus status,
        string defaultPayoutMethod = "Tài khoản ngân hàng",
        string defaultPayoutBank = "Vietcombank")
    {
        Id = id;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Skill = skill;
        Description = description;
        Experience = experience;
        PricePerSession = pricePerSession;
        TeachingMode = teachingMode;
        OfflineLocation = offlineLocation;
        PortfolioImageUrl = portfolioImageUrl;
        Status = status;
        DefaultPayoutMethod = defaultPayoutMethod;
        DefaultPayoutBank = defaultPayoutBank;
    }

    public Guid? Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Skill { get; set; } = "";
    public string Description { get; set; } = "";
    public string Experience { get; set; } = "";
    public decimal PricePerSession { get; set; }
    public TeachingMode TeachingMode { get; set; } = TeachingMode.Online;
    public string OfflineLocation { get; set; } = "";
    public string PortfolioImageUrl { get; set; } = "";
    public TeacherProfileStatus Status { get; set; } = TeacherProfileStatus.Draft;
    public string DefaultPayoutMethod { get; set; } = "Tài khoản ngân hàng";
    public string DefaultPayoutBank { get; set; } = "Vietcombank";
}

public sealed record TeacherBookingRowViewModel(
    Guid BookingId,
    Guid StudentId,
    string StudentName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string LessonContent,
    BookingStatus Status,
    TeachingMode TeachingMode,
    string OfflineLocation,
    bool StudentCompleted,
    bool TeacherCompleted,
    bool IsUnderReview = false);

public sealed record TeacherStudentProgressViewModel(
    Guid StudentId,
    string StudentName,
    string LessonContent,
    int RegisteredSessions,
    int CompletedSessions,
    DateTimeOffset? NextSession,
    string LearningStatus,
    string PaymentStatus,
    int ProgressPercent,
    string TeacherNote,
    IEnumerable<TeacherStudentSessionViewModel> Sessions,
    IEnumerable<TeacherScheduleChangeRequestViewModel> ScheduleChangeRequests);

public sealed record TeacherStudentSessionViewModel(
    Guid BookingId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string DayName,
    BookingStatus Status,
    string LessonContent,
    TeachingMode TeachingMode,
    string OfflineLocation,
    bool StudentCompleted,
    bool TeacherCompleted,
    string CompletionStatus,
    string ProgressNote,
    bool IsUnderReview = false);

public sealed record TeacherDayScheduleViewModel(
    DayOfWeek DayOfWeek,
    string DayName,
    IEnumerable<TeacherDayScheduleStudentViewModel> Students);

public sealed record TeacherDayScheduleStudentViewModel(
    Guid BookingId,
    string StudentName,
    string LessonContent,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingStatus Status,
    string PaymentStatus,
    bool CanJoinRoom,
    TeachingMode TeachingMode,
    string OfflineLocation,
    bool StudentCompleted,
    bool TeacherCompleted,
    bool IsUnderReview = false);

public sealed record TeacherScheduleChangeRequestViewModel(
    Guid RequestId,
    Guid BookingId,
    string StudentName,
    DateTimeOffset CurrentStartTime,
    DateTimeOffset? RequestedStartTime,
    string Type,
    string Reason,
    string Status,
    bool IsEligibleByTimeRule);

public sealed record TeacherAvailabilityDayViewModel(
    DayOfWeek DayOfWeek,
    bool IsActive,
    IEnumerable<TeacherAvailabilitySlotViewModel> Slots);

public sealed record TeacherAvailabilitySlotViewModel(
    Guid Id,
    string StartTime,
    string EndTime,
    bool IsActive,
    string PlannedContent,
    TeachingMode TeachingMode,
    string OfflineLocation);

public sealed record TeacherDetailViewModel(
    TeacherResponse Teacher,
    IEnumerable<BookingSlotViewModel> Slots,
    string? Error = null);

public sealed record BookingSlotViewModel(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsBooked,
    TeachingMode TeachingMode,
    string OfflineLocation,
    string LessonContent);

public sealed record AdminDashboardViewModel(
    int TotalStudents,
    int TotalTeachers,
    int TotalBookings,
    int TotalTransactions,
    decimal SystemCommission,
    int PendingComplaints,
    IEnumerable<AdminComplaintViewModel> Complaints,
    IEnumerable<User> Students,
    IEnumerable<User> Teachers,
    AdminTransactionOverviewViewModel Transactions,
    IEnumerable<ChatContactViewModel> SupportContacts,
    string? Success = null,
    string? Error = null);

public sealed record AdminComplaintViewModel(
    Guid Id,
    Guid BookingId,
    string StudentName,
    string TeacherName,
    string LessonContent,
    decimal Amount,
    string Reason,
    string StudentEvidenceUrl,
    string TeacherResponse,
    string TeacherEvidenceUrl,
    ComplaintStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? TeacherRespondedAt,
    bool IsAutoRefundDue);

public sealed record AdminTransactionOverviewViewModel(
    decimal Held,
    decimal Released,
    decimal Refunded,
    decimal Commission);

public sealed record WalletCheckoutViewModel(
    Guid UserId,
    UserRole Role,
    string UserName,
    decimal Balance,
    decimal Amount = 0,
    string Method = "Visa / Mastercard",
    string TransactionCode = "",
    string Status = "",
    bool IsConfirmStep = false,
    string? Success = null,
    string? Error = null);
