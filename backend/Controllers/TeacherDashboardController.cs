using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using SkillBridge.Api.DTOs;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class TeacherDashboardController(SkillBridgeService service, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public IActionResult Index(string? success = null)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (CurrentUserRole() != UserRole.Teacher)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(BuildModel(userId.Value, success));
    }

    [HttpGet]
    public IActionResult Students(string? success = null)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (CurrentUserRole() != UserRole.Teacher)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(BuildModel(userId.Value, success));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Profile([Bind(Prefix = "Profile")] TeacherProfileEditViewModel profile, IFormFile? avatarFile, IFormFile? portfolioFile)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var existingProfile = service.TeacherProfileForUser(userId.Value);
            var existingUser = service.UserById(userId.Value);
            var avatarUrl = SaveImage(avatarFile, userId.Value, "avatar") ?? profile.AvatarUrl ?? existingUser?.AvatarUrl ?? "";
            var portfolioImageUrl = SaveImage(portfolioFile, userId.Value, "portfolio") ?? profile.PortfolioImageUrl ?? existingProfile?.PortfolioImageUrl ?? "";

            service.SaveTeacherProfile(
                userId.Value,
                profile.DisplayName,
                avatarUrl,
                profile.Skill,
                profile.Description,
                profile.Experience,
                profile.PricePerSession,
                profile.TeachingMode,
                profile.OfflineLocation,
                portfolioImageUrl,
                profile.Status,
                profile.DefaultPayoutMethod,
                profile.DefaultPayoutBank);

            Response.Cookies.Append($"{AuthSession.Prefix(UserRole.Teacher)}_user_name", profile.DisplayName, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });
            return RedirectToAction(nameof(Index), new { success = "Đã lưu hồ sơ giáo viên." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId.Value, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirm(Guid id, string? returnTo = null)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.ConfirmBooking(id, new BookingDecisionRequest(userId));
            return RedirectAfterStudentAction(returnTo, "Đã xác nhận lịch học.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reject(Guid id, string? returnTo = null)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.RejectBooking(id, new BookingDecisionRequest(userId));
            return RedirectAfterStudentAction(returnTo, "Đã từ chối lịch học.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CompleteTeaching(Guid id, string? returnTo = null)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.TeacherComplete(id, new TeacherCompleteBookingRequest(userId));
            return RedirectAfterStudentAction(returnTo, "Đã ghi nhận xác nhận đã dạy.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RespondComplaint(Guid id, string response = "", IFormFile? evidenceFile = null)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var evidenceUrl = SaveImage(evidenceFile, userId, "complaint-evidence") ?? "";
            service.RespondToComplaint(id, new TeacherComplaintResponseRequest(userId, response, evidenceUrl));
            return RedirectToAction(nameof(Students), new { success = "Đã gửi phản hồi khiếu nại." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Students", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RequestWithdrawal(decimal amount, string method = "", string accountName = "", string accountNumber = "", string bankName = "")
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.RequestWithdrawal(userId, amount, method, accountName, accountNumber, bankName);
            return Redirect($"{Url.Action(nameof(Index), new { success = "Đã tạo yêu cầu rút tiền." })}#payments");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarkWithdrawalPaid(Guid id)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.MarkWithdrawalPaid(userId, id);
            return Redirect($"{Url.Action(nameof(Index), new { success = "Đã đánh dấu yêu cầu rút tiền là đã rút." })}#payments");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveAvailability(Guid? id, DayOfWeek dayOfWeek, string startTime, string endTime, bool isActive = true, string plannedContent = "", TeachingMode teachingMode = TeachingMode.Online, string offlineLocation = "")
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
            {
                throw new InvalidOperationException("Vui lòng nhập giờ bắt đầu và giờ kết thúc hợp lệ.");
            }

            service.SaveAvailability(userId, dayOfWeek, start, end, isActive, plannedContent, teachingMode, offlineLocation, id);
            return Redirect($"{Url.Action(nameof(Index), new { success = "Đã cập nhật lịch dạy." })}#schedule");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveSchedule(List<TeacherAvailabilityEditInput> slots, List<TeacherAvailabilityEditInput> newSlots)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            foreach (var slot in slots.Where(slot => slot.Id is not null))
            {
                SaveScheduleSlot(userId, slot);
            }

            foreach (var slot in newSlots.Where(slot =>
                !string.IsNullOrWhiteSpace(slot.StartTime) &&
                !string.IsNullOrWhiteSpace(slot.EndTime)))
            {
                SaveScheduleSlot(userId, slot with { Id = null, IsActive = true });
            }

            return Redirect($"{Url.Action(nameof(Index), new { success = "Đã lưu lịch dạy." })}#schedule");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAvailability(Guid id)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.DeleteAvailability(userId, id);
            return Redirect($"{Url.Action(nameof(Index), new { success = "Đã xóa khung giờ dạy." })}#schedule");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleDay(DayOfWeek dayOfWeek, bool isActive)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        service.SetDayAvailability(userId, dayOfWeek, isActive);
        return Redirect($"{Url.Action(nameof(Index), new { success = "Đã cập nhật trạng thái ngày dạy." })}#schedule");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcceptScheduleChange(Guid id)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.AcceptScheduleChange(id, userId);
            return Redirect($"{Url.Action(nameof(Index), new { success = "Đã chấp nhận yêu cầu đổi lịch/hủy buổi." })}#bookings");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectScheduleChange(Guid id)
    {
        if (CurrentUserId() is not { } userId)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.RejectScheduleChange(id, userId);
            return Redirect($"{Url.Action(nameof(Index), new { success = "Đã từ chối yêu cầu đổi lịch/hủy buổi." })}#bookings");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId, error: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveProgressNote(Guid studentId, string note)
    {
        if (CurrentUserId() is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return Redirect($"{Url.Action(nameof(Index), new { success = "Đã lưu ghi chú tiến độ học tập." })}#bookings");
    }

    private TeacherDashboardViewModel BuildModel(Guid userId, string? success = null, string? error = null)
    {
        var user = service.UserById(userId);
        var profile = service.TeacherProfileForUser(userId);
        var rows = service.TeacherBookingRows(userId)
            .Select(row => new TeacherBookingRowViewModel(
                row.Booking.Id,
                row.Student.Id,
                row.Student.Name,
                row.Booking.StartTime,
                row.Booking.EndTime,
                string.IsNullOrWhiteSpace(row.Booking.LessonContent) ? "Ôn tập mục tiêu và luyện đề" : row.Booking.LessonContent,
                row.Booking.Status,
                row.Booking.TeachingMode,
                row.Booking.OfflineLocation,
                row.Booking.StudentCompleted,
                row.Booking.TeacherCompleted,
                service.TransactionsForBooking(row.Booking.Id).Any(transaction => transaction.Status == TransactionStatus.UnderReview)))
            .ToList();
        var registeredStudents = rows.Select(row => row.StudentName).Distinct().Count();
        var completedRows = rows.Where(row => row.Status == BookingStatus.Completed).ToList();
        var reviews = service.ReviewsForTeacher(userId).ToList();
        var studentNamesById = rows
            .GroupBy(row => row.StudentId)
            .ToDictionary(group => group.Key, group => group.First().StudentName);
        var recentReviews = reviews
            .Take(6)
            .Select(review => new TeacherReviewViewModel(
                studentNamesById.TryGetValue(review.StudentId, out var studentName) ? studentName : "Học viên",
                review.Stars,
                string.IsNullOrWhiteSpace(review.Comment) ? "Học viên đã gửi đánh giá." : review.Comment,
                review.CreatedAt))
            .ToList();
        var professionalOverview = new TeacherProfessionalOverviewViewModel(
            reviews.Count > 0 ? Math.Round(reviews.Average(review => (decimal)review.Stars), 1) : profile?.Rating ?? 0m,
            reviews.Count,
            completedRows.Count,
            completedRows.Select(row => row.StudentName).Distinct().Count(),
            completedRows.Count * (profile?.PricePerSession ?? 0),
            recentReviews);
        var scheduleRequests = service.ScheduleChangeRequestsForTeacher(userId).ToList();
        var paymentOverview = BuildPaymentOverview(rows, profile?.PricePerSession ?? 0);
        var studentProgress = BuildStudentProgress(rows, scheduleRequests);
        var daySchedules = BuildDaySchedules(rows);
        var availabilityRows = service.AvailabilitiesForTeacher(userId).ToList();
        var availabilityDays = Enum.GetValues<DayOfWeek>()
            .Select(day =>
            {
                var slots = availabilityRows.Where(a => a.DayOfWeek == day).ToList();
                return new TeacherAvailabilityDayViewModel(
                    day,
                    slots.Count == 0 || slots.Any(slot => slot.IsActive),
                    slots.Select(slot => new TeacherAvailabilitySlotViewModel(
                        slot.Id,
                        slot.StartTime.ToString(@"hh\:mm"),
                        slot.EndTime.ToString(@"hh\:mm"),
                        slot.IsActive,
                        slot.PlannedContent,
                        slot.TeachingMode,
                        slot.OfflineLocation)));
            })
            .ToList();

        var editModel = new TeacherProfileEditViewModel(
            profile?.Id,
            user?.Name ?? "",
            user?.AvatarUrl ?? "",
            profile?.Skill ?? "",
            profile?.Description ?? user?.Bio ?? "",
            profile?.Experience ?? "",
            profile?.PricePerSession ?? 0,
            profile?.TeachingMode ?? TeachingMode.Online,
            profile?.OfflineLocation ?? "",
            profile?.PortfolioImageUrl ?? "",
            profile?.Status ?? TeacherProfileStatus.Active,
            profile?.DefaultPayoutMethod ?? "Tài khoản ngân hàng",
            profile?.DefaultPayoutBank ?? "Vietcombank");

        return new TeacherDashboardViewModel(
            editModel,
            profile is not null,
            registeredStudents,
            rows.Count(row => row.Status == BookingStatus.Pending),
            rows.Count(row => row.Status == BookingStatus.Confirmed || row.Status == BookingStatus.Paid || row.Status == BookingStatus.InProgress),
            rows.Count(row => row.Status == BookingStatus.Completed),
            professionalOverview,
            paymentOverview,
            rows,
            studentProgress,
            daySchedules,
            availabilityDays,
            success,
            error);
    }

    private TeacherPaymentOverviewViewModel BuildPaymentOverview(IEnumerable<TeacherBookingRowViewModel> rows, decimal pricePerSession)
    {
        var paidRows = rows
            .Where(row => row.Status is BookingStatus.Paid or BookingStatus.InProgress or BookingStatus.Completed)
            .OrderByDescending(row => row.StartTime)
            .ToList();
        var transactions = paidRows
            .Select(row =>
            {
                var dbTransactions = service.TransactionsForBooking(row.BookingId).ToList();
                var latest = dbTransactions.FirstOrDefault();
                var total = dbTransactions
                    .Where(transaction => transaction.Type == TransactionType.Hold)
                    .OrderBy(transaction => transaction.CreatedAt)
                    .Select(transaction => transaction.Amount)
                    .FirstOrDefault();
                if (total <= 0)
                {
                    total = pricePerSession;
                }

                var commission = total * 0.10m;
                var invoice = service.InvoiceForBooking(row.BookingId);
                var status = latest?.Status switch
                {
                    TransactionStatus.Released => "Đã trả cho giáo viên",
                    TransactionStatus.UnderReview => "Đang xem xét khiếu nại",
                    TransactionStatus.Refunded => "Đã hoàn tiền",
                    _ => row.Status == BookingStatus.Completed ? "Đã trả cho giáo viên" : "Đang giữ tiền"
                };

                return new TeacherPaymentTransactionViewModel(
                    row.BookingId,
                    row.StudentName,
                    1,
                    total,
                    commission,
                    total - commission,
                    status,
                    row.StartTime,
                    invoice?.PaymentMethod ?? "Ví SkillBridge",
                    invoice?.InvoiceCode ?? row.BookingId.ToString("N")[..10].ToUpperInvariant());
            })
            .ToList();

        var totalPaid = transactions.Sum(transaction => transaction.TotalAmount);
        var totalHeld = transactions
            .Where(transaction => transaction.Status == "Đang giữ tiền")
            .Sum(transaction => transaction.TotalAmount);
        var totalReleased = transactions
            .Where(transaction => transaction.Status == "Đã trả cho giáo viên")
            .Sum(transaction => transaction.TotalAmount);
        var commissionTotal = transactions.Sum(transaction => transaction.CommissionAmount);
        var teacherNet = transactions
            .Where(transaction => transaction.Status == "Đã trả cho giáo viên")
            .Sum(transaction => transaction.TeacherNetAmount);
        var withdrawals = service.WithdrawalsForTeacher(CurrentUserId() ?? Guid.Empty)
            .Select(withdrawal => new TeacherWithdrawalViewModel(
                withdrawal.Id,
                withdrawal.Amount,
                withdrawal.Status == WithdrawalStatus.Paid ? "Đã rút" : "Đang xử lý",
                withdrawal.CreatedAt,
                withdrawal.Method,
                withdrawal.AccountName,
                withdrawal.AccountNumber,
                withdrawal.BankName))
            .ToList();
        var pendingWithdrawal = withdrawals
            .Where(withdrawal => withdrawal.Status == "Đang xử lý")
            .Sum(withdrawal => withdrawal.Amount);
        var totalWithdrawn = withdrawals
            .Where(withdrawal => withdrawal.Status == "Đã rút")
            .Sum(withdrawal => withdrawal.Amount);
        var withdrawable = Math.Max(0, teacherNet - pendingWithdrawal - totalWithdrawn);

        return new TeacherPaymentOverviewViewModel(
            totalPaid,
            totalHeld,
            totalReleased,
            commissionTotal,
            teacherNet,
            teacherNet,
            withdrawable,
            totalWithdrawn,
            pendingWithdrawal,
            transactions,
            withdrawals);
    }

    private static List<TeacherStudentProgressViewModel> BuildStudentProgress(
        IEnumerable<TeacherBookingRowViewModel> rows,
        IEnumerable<ScheduleChangeRequest> scheduleRequests)
    {
        var now = DateTimeOffset.Now;
        return rows
            .GroupBy(row => new { row.StudentId, row.StudentName })
            .Select(group =>
            {
                var ordered = group.OrderBy(row => row.StartTime).ToList();
                var registered = ordered.Count;
                var completed = ordered.Count(row => row.Status == BookingStatus.Completed);
                var nextSession = ordered
                    .Where(row => row.StartTime > now && row.Status is BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.Paid)
                    .OrderBy(row => row.StartTime)
                    .Select(row => (DateTimeOffset?)row.StartTime)
                    .FirstOrDefault();
                var progress = registered == 0 ? 0 : (int)Math.Round(completed * 100m / registered);
                var status = ordered.Any(row => row.Status == BookingStatus.InProgress)
                    ? "WaitingOtherSide"
                    : nextSession is not null
                        ? "InProgress"
                        : completed > 0 ? "Completed" : "NotStarted";
                var paymentStatus = ordered.Any(row => row.IsUnderReview)
                    ? "TeacherDisputeStatus"
                    : ordered.Any(row => row.Status is BookingStatus.Paid or BookingStatus.InProgress or BookingStatus.Completed)
                        ? "PaidPaymentExists"
                        : "NoPaymentYet";
                var lessonContent = ordered
                    .GroupBy(row => row.LessonContent)
                    .OrderByDescending(lessonGroup => lessonGroup.Count())
                    .First().Key;
                var sessions = ordered.Select(row => new TeacherStudentSessionViewModel(
                    row.BookingId,
                    row.StartTime,
                    row.EndTime,
                    DayName(row.StartTime.DayOfWeek),
                    row.Status,
                    row.LessonContent,
                    row.TeachingMode,
                    row.OfflineLocation,
                    row.StudentCompleted,
                    row.TeacherCompleted,
                    CompletionStatus(row.StudentCompleted, row.TeacherCompleted, row.Status),
                    row.Status == BookingStatus.Completed
                        ? "CompletedLessonNote"
                        : "NoPostLessonNote",
                    row.IsUnderReview));
                var requests = scheduleRequests
                    .Where(request => request.StudentId == group.Key.StudentId)
                    .OrderByDescending(request => request.CreatedAt)
                    .Select(request => new TeacherScheduleChangeRequestViewModel(
                        request.Id,
                        request.BookingId,
                        group.Key.StudentName,
                        request.CurrentStartTime,
                        request.RequestedStartTime,
                        request.Type == ScheduleChangeType.Cancel ? "Hủy buổi" : "Đổi lịch",
                        request.Reason,
                        ScheduleChangeStatusLabel(request.Status),
                        request.CurrentStartTime - now >= TimeSpan.FromDays(2)))
                    .ToList();

                return new TeacherStudentProgressViewModel(
                    group.Key.StudentId,
                    group.Key.StudentName,
                    lessonContent,
                    registered,
                    completed,
                    nextSession,
                    status,
                    paymentStatus,
                    progress,
                    completed > 0
                        ? "Học viên có tiến độ ổn định, cần duy trì lịch học đều."
                        : "Chưa có đủ dữ liệu để đánh giá tiến độ.",
                    sessions,
                    requests);
            })
            .OrderBy(student => student.StudentName)
            .ToList();
    }

    private static List<TeacherDayScheduleViewModel> BuildDaySchedules(IEnumerable<TeacherBookingRowViewModel> rows)
    {
        var activeStatuses = new[] { BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.Paid, BookingStatus.InProgress };
        return Enum.GetValues<DayOfWeek>()
            .Select(day => new TeacherDayScheduleViewModel(
                day,
                DayName(day),
                rows
                    .Where(row => row.StartTime.DayOfWeek == day && activeStatuses.Contains(row.Status))
                    .OrderBy(row => row.StartTime.TimeOfDay)
                    .Select(row => new TeacherDayScheduleStudentViewModel(
                        row.BookingId,
                        row.StudentName,
                        row.LessonContent,
                        row.StartTime,
                        row.EndTime,
                        row.Status,
                        row.Status is BookingStatus.Paid or BookingStatus.InProgress or BookingStatus.Completed
                            ? "PaidStatus"
                            : "UnpaidStatus",
                        row.TeachingMode is TeachingMode.Online or TeachingMode.Hybrid &&
                            row.Status is (BookingStatus.Paid or BookingStatus.InProgress) &&
                            DateTimeOffset.Now >= row.StartTime.AddMinutes(-15) &&
                            DateTimeOffset.Now <= row.EndTime.AddMinutes(30),
                        row.TeachingMode,
                        row.OfflineLocation,
                        row.StudentCompleted,
                        row.TeacherCompleted,
                        row.IsUnderReview))
                    .ToList()))
            .ToList();
    }

    private static string DayName(DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            _ => "Chủ nhật"
        };

    private static string ScheduleChangeStatusLabel(ScheduleChangeStatus status) =>
        status switch
        {
            ScheduleChangeStatus.Accepted => "Đã chấp nhận",
            ScheduleChangeStatus.Rejected => "Đã từ chối",
            _ => "Đang chờ giáo viên xử lý"
        };

    private static string CompletionStatus(bool studentCompleted, bool teacherCompleted, BookingStatus status)
    {
        if (status == BookingStatus.Completed)
        {
            return "Completed";
        }

        if (studentCompleted && !teacherCompleted)
        {
            return "WaitingOtherSide";
        }

        if (!studentCompleted && teacherCompleted)
        {
            return "WaitingOtherSide";
        }

        return "InProgress";
    }

    private static List<TeacherReviewViewModel> BuildMockReviews(IEnumerable<TeacherBookingRowViewModel> rows)
    {
        var studentNames = rows
            .Select(row => row.StudentName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Take(3)
            .ToList();

        if (studentNames.Count == 0)
        {
            studentNames = ["Minh Anh", "Gia Huy", "Thu Ha"];
        }

        var comments = new[]
        {
            "Giáo viên giải thích rõ, bài học dễ theo dõi.",
            "Nội dung thực tế, có ví dụ phù hợp với mục tiêu học.",
            "Phản hồi nhanh và chuẩn bị tài liệu cẩn thận."
        };

        return studentNames
            .Select((name, index) => new TeacherReviewViewModel(
                name,
                index == 1 ? 4 : 5,
                comments[index % comments.Length],
                DateTimeOffset.Now.AddDays(-(index + 1))))
            .ToList();
    }

    private Guid? CurrentUserId() =>
        AuthSession.UserId(HttpContext, UserRole.Teacher);

    private UserRole? CurrentUserRole() =>
        AuthSession.IsSignedIn(HttpContext, UserRole.Teacher) ? UserRole.Teacher : null;

    private IActionResult RedirectAfterStudentAction(string? returnTo, string success) =>
        string.Equals(returnTo, "Students", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction(nameof(Students), new { success })
            : Redirect($"{Url.Action(nameof(Index), new { success })}#bookings");

    private string? SaveImage(IFormFile? file, Guid userId, string slot)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vui lòng tải lên tệp hình ảnh.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException("Ảnh tải lên phải có dung lượng tối đa 5MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Định dạng ảnh được hỗ trợ: JPG, PNG, WEBP, GIF.");
        }

        var uploadsRoot = Path.Combine(environment.WebRootPath, "uploads", "teachers");
        Directory.CreateDirectory(uploadsRoot);
        var fileName = $"{userId:N}-{slot}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        using var stream = System.IO.File.Create(absolutePath);
        file.CopyTo(stream);

        return $"/uploads/teachers/{fileName}";
    }

    private void SaveScheduleSlot(Guid userId, TeacherAvailabilityEditInput slot)
    {
        if (!TimeSpan.TryParse(slot.StartTime, out var start) || !TimeSpan.TryParse(slot.EndTime, out var end))
        {
            throw new InvalidOperationException("Vui lòng nhập giờ bắt đầu và giờ kết thúc hợp lệ.");
        }

        service.SaveAvailability(userId, slot.DayOfWeek, start, end, slot.IsActive, slot.PlannedContent ?? "", slot.TeachingMode, slot.OfflineLocation ?? "", slot.Id);
    }
}

public sealed record TeacherAvailabilityEditInput
{
    public Guid? Id { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public string StartTime { get; init; } = "";
    public string EndTime { get; init; } = "";
    public string PlannedContent { get; init; } = "";
    public bool IsActive { get; init; }
    public TeachingMode TeachingMode { get; init; } = TeachingMode.Online;
    public string OfflineLocation { get; init; } = "";
}
