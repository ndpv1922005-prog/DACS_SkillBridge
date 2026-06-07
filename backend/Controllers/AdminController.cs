using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class AdminController(SkillBridgeService service) : Controller
{
    public IActionResult Index(string? success = null, string? error = null)
    {
        var adminId = CurrentAdminId();
        if (adminId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(BuildModel(adminId.Value, success, error));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LockUser(Guid id)
    {
        if (CurrentAdminId() is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.SetUserLocked(id, true);
            return RedirectToAction(nameof(Index), new { success = "Đã khóa tài khoản." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return RedirectToAction(nameof(Index), new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UnlockUser(Guid id)
    {
        if (CurrentAdminId() is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.SetUserLocked(id, false);
            return RedirectToAction(nameof(Index), new { success = "Đã mở khóa tài khoản." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return RedirectToAction(nameof(Index), new { error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResolveComplaint(Guid id, string decision)
    {
        if (CurrentAdminId() is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.ResolveComplaint(id, decision);
            return RedirectToAction(nameof(Index), new { success = "Đã cập nhật khiếu nại." });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return RedirectToAction(nameof(Index), new { error = ex.Message });
        }
    }

    private AdminDashboardViewModel BuildModel(Guid adminId, string? success, string? error)
    {
        var students = service.UsersByRole(UserRole.Student).ToList();
        var teachers = service.UsersByRole(UserRole.Teacher).ToList();
        var bookings = service.AllBookings().ToList();
        var transactions = service.AllTransactions().ToList();
        var teachersById = service.SearchTeachers(null, null).ToDictionary(teacher => teacher.UserId);
        var complaints = service.AllComplaints().ToList();

        decimal PriceFor(Booking booking) =>
            teachersById.TryGetValue(booking.TeacherId, out var teacher) ? teacher.PricePerSession : 0;

        var complaintRows = complaints.Select(complaint =>
        {
            var booking = bookings.FirstOrDefault(item => item.Id == complaint.BookingId);
            return new AdminComplaintViewModel(
                complaint.Id,
                complaint.BookingId,
                students.FirstOrDefault(user => user.Id == complaint.StudentId)?.Name ?? "Học viên",
                teachers.FirstOrDefault(user => user.Id == complaint.TeacherId)?.Name ?? "Giáo viên",
                booking?.LessonContent ?? "Buổi học",
                booking is null ? 0 : PriceFor(booking),
                complaint.Reason,
                complaint.StudentEvidenceUrl,
                complaint.TeacherResponse,
                complaint.TeacherEvidenceUrl,
                complaint.Status,
                complaint.CreatedAt,
                complaint.TeacherRespondedAt,
                complaint.Status == ComplaintStatus.WaitingTeacherResponse &&
                    complaint.TeacherRespondedAt is null &&
                    complaint.CreatedAt <= DateTimeOffset.UtcNow.AddDays(-7));
        }).ToList();

        var held = transactions.Where(transaction => transaction.Status is TransactionStatus.Held or TransactionStatus.UnderReview).Sum(transaction => transaction.Amount);
        var released = transactions.Where(transaction => transaction.Status == TransactionStatus.Released).Sum(transaction => transaction.Amount);
        var refunded = transactions.Where(transaction => transaction.Status == TransactionStatus.Refunded).Sum(transaction => transaction.Amount);
        var commission = released * 0.10m;
        var supportContacts = service.ChatContactsForUser(adminId, UserRole.Admin)
            .Select(user => new ChatContactViewModel(
                user.Id,
                user.Name,
                user.Role == UserRole.Teacher ? "Giáo viên" : "Học viên",
                user.AvatarUrl,
                service.ConversationForViewer(adminId, user.Id).LastOrDefault()?.Content ?? "Chưa có tin nhắn hỗ trợ",
                service.ConversationForViewer(adminId, user.Id).LastOrDefault()?.CreatedAt,
                service.UnreadMessagesFromContact(adminId, user.Id)))
            .OrderByDescending(contact => contact.LastMessageAt ?? DateTimeOffset.MinValue)
            .Take(8)
            .ToList();

        return new AdminDashboardViewModel(
            students.Count,
            teachers.Count,
            bookings.Count,
            transactions.Count,
            commission,
            complaintRows.Count(row => row.Status is ComplaintStatus.WaitingTeacherResponse or ComplaintStatus.InReview),
            complaintRows,
            students,
            teachers,
            new AdminTransactionOverviewViewModel(held, released, refunded, commission),
            supportContacts,
            success,
            error);
    }

    private Guid? CurrentAdminId() => AuthSession.UserId(HttpContext, UserRole.Admin);
}
