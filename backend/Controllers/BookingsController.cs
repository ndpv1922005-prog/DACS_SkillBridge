using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using SkillBridge.Api.DTOs;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class BookingsController(SkillBridgeService service, IWebHostEnvironment environment) : Controller
{
    public IActionResult Index()
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new BookingListViewModel(
            service.BookingsForUser(userId.Value),
            service.SearchTeachers(null, null),
            service.ScheduleChangeRequestsForStudent(userId.Value),
            userId,
            service.BookingsForUser(userId.Value)
                .Where(booking => service.TransactionsForBooking(booking.Id).Any(transaction => transaction.Status == TransactionStatus.UnderReview))
                .Select(booking => booking.Id)
                .ToList(),
            service.ReviewedBookingIdsForStudent(userId.Value));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Guid teacherProfileId, DateTimeOffset startTime, TeachingMode teachingMode = TeachingMode.Online)
    {
        if (AuthSession.IsSignedIn(HttpContext, UserRole.Teacher))
        {
            return RedirectToAction("Index", "TeacherDashboard", new { success = "Giáo viên không thể đặt lịch học. Vui lòng dùng tài khoản học viên nếu muốn đăng ký học." });
        }

        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.CreateBooking(new BookingRequest(userId.Value, teacherProfileId, startTime, 60, teachingMode));
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return RedirectToAction("Details", "Teachers", new { id = teacherProfileId, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Pay(Guid id, string method = "Ví SkillBridge")
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.Pay(id, new PaymentRequest(userId.Value, method));
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Complete(Guid id)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.Complete(id, new CompleteBookingRequest(userId.Value));
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RequestScheduleChange(Guid id, string type, DateTimeOffset? requestedStartTime, string reason = "")
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var changeType = string.Equals(type, "Cancel", StringComparison.OrdinalIgnoreCase)
                ? ScheduleChangeType.Cancel
                : ScheduleChangeType.Reschedule;
            service.RequestScheduleChange(id, userId.Value, changeType, requestedStartTime, reason);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Dispute(Guid id, string reason = "", string otherReason = "", IFormFile? evidenceFile = null)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var finalReason = reason == "Khác" || reason == "KhĂ¡c" || reason.Equals("Other", StringComparison.OrdinalIgnoreCase)
                ? otherReason
                : reason;
            var evidenceUrl = SaveImage(evidenceFile, userId.Value, "complaint-evidence") ?? "";
            service.DisputePayment(id, new ComplaintRequest(userId.Value, finalReason, evidenceUrl));
            TempData["Success"] = "Đã gửi khiếu nại.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Review(Guid id, int stars, string comment = "")
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.ReviewTeacher(id, new ReviewTeacherRequest(userId.Value, stars, comment));
            TempData["Success"] = "Đã gửi đánh giá giáo viên.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public IActionResult Room(Guid id)
    {
        var role = AuthSession.RoleFor(HttpContext);
        var userId = role is null ? null : AuthSession.UserId(HttpContext, role.Value);
        if (userId is null && AuthSession.IsSignedIn(HttpContext, UserRole.Teacher))
        {
            role = UserRole.Teacher;
            userId = AuthSession.UserId(HttpContext, UserRole.Teacher);
        }

        if (userId is null && AuthSession.IsSignedIn(HttpContext, UserRole.Student))
        {
            role = UserRole.Student;
            userId = AuthSession.UserId(HttpContext, UserRole.Student);
        }

        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var booking = service.BookingForParticipant(id, userId.Value);
        if (booking.TeachingMode is not (TeachingMode.Online or TeachingMode.Hybrid))
        {
            return RedirectToAction(nameof(Index));
        }

        if (!service.CanJoinCall(id, userId.Value))
        {
            return RedirectToAction(nameof(Index));
        }

        return View(booking);
    }

    private Guid? CurrentUserId() =>
        AuthSession.UserId(HttpContext, UserRole.Student);

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

        var uploadsRoot = Path.Combine(environment.WebRootPath, "uploads", "complaints");
        Directory.CreateDirectory(uploadsRoot);
        var fileName = $"{userId:N}-{slot}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        using var stream = System.IO.File.Create(absolutePath);
        file.CopyTo(stream);

        return $"/uploads/complaints/{fileName}";
    }
}
