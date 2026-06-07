using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class DashboardController(SkillBridgeService service, IWebHostEnvironment environment) : Controller
{
    public IActionResult Index()
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Student);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = service.UserById(userId.Value);
        var model = BuildModel(
            userId.Value,
            user,
            TempData["Success"] as string,
            TempData["Error"] as string);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Profile(StudentProfileEditViewModel profile, IFormFile? avatarFile)
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Student);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var existing = service.UserById(userId.Value);
            if (existing is null)
            {
                TempData["Error"] = UiText.T(HttpContext, "ERR_USER_NOT_FOUND");
                return RedirectToAction(nameof(Index));
            }

            var avatarUrl = SaveAvatar(avatarFile, userId.Value) ?? profile.AvatarUrl ?? existing.AvatarUrl;
            var user = service.SaveStudentProfile(
                userId.Value,
                profile.DisplayName,
                profile.Email,
                avatarUrl,
                profile.Bio,
                profile.Gender,
                profile.BirthDate,
                profile.Phone,
                profile.LearningGoal);
            Response.Cookies.Append($"{AuthSession.Prefix(UserRole.Student)}_user_name", user.Name, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });
            TempData["Success"] = UiText.T(HttpContext, "ProfileSaved");
            return Redirect($"{Url.Action(nameof(Index))}#profile");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View("Index", BuildModel(userId.Value, service.UserById(userId.Value), error: UiText.T(HttpContext, ex.Message)));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TopUp(decimal amount, string method = "Momo")
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Student);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (amount <= 0)
        {
            return View("Index", BuildModel(userId.Value, service.UserById(userId.Value), error: UiText.T(HttpContext, "TopUpInvalid")));
        }

        service.TopUpWallet(userId.Value, amount, method);

        TempData["Success"] = "Đã nạp tiền thành công.";
        return Redirect($"{Url.Action(nameof(Index))}#payments");
    }

    private StudentDashboardViewModel BuildModel(Guid userId, User? user, string? success = null, string? error = null)
    {
        var storedProfile = service.StudentProfileForUser(userId);
        var profile = new StudentProfileEditViewModel
        {
            DisplayName = storedProfile?.DisplayName ?? user?.Name ?? "",
            AvatarUrl = storedProfile?.AvatarUrl ?? user?.AvatarUrl ?? "",
            Email = storedProfile?.Email ?? user?.Email ?? "",
            Bio = storedProfile?.Bio ?? user?.Bio ?? "",
            Gender = storedProfile?.Gender ?? Request.Cookies[ProfileCookieName(userId, "gender")] ?? "",
            BirthDate = storedProfile?.DateOfBirth?.ToString("yyyy-MM-dd") ?? Request.Cookies[ProfileCookieName(userId, "birthDate")] ?? "",
            Phone = storedProfile?.Phone ?? Request.Cookies[ProfileCookieName(userId, "phone")] ?? "",
            LearningGoal = storedProfile?.LearningGoal ?? Request.Cookies[ProfileCookieName(userId, "learningGoal")] ?? ""
        };

        var bookings = service.BookingsForUser(userId).ToList();
        var teachers = service.SearchTeachers(null, null).ToList();
        var invoices = service.InvoicesForUser(userId).ToList();
        var walletTransactions = service.WalletTransactionsForUser(userId).ToList();
        var held = invoices.Where(invoice => invoice.Status == InvoiceStatus.Held).Sum(invoice => invoice.Amount);
        var paid = invoices.Where(invoice => invoice.Status == InvoiceStatus.Completed).Sum(invoice => invoice.Amount);
        var refunded = invoices.Where(invoice => invoice.Status == InvoiceStatus.Refunded).Sum(invoice => invoice.Amount);
        var wallet = service.StudentWalletBalance(userId);

        return new StudentDashboardViewModel(
            teachers,
            bookings,
            profile,
            wallet,
            held,
            paid,
            refunded,
            invoices,
            walletTransactions,
            success,
            error);
    }

    private string? SaveAvatar(IFormFile? file, Guid userId)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vui lòng tải lên tệp hình ảnh.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Định dạng ảnh được hỗ trợ: JPG, PNG, WEBP, GIF.");
        }

        var uploadsRoot = Path.Combine(environment.WebRootPath, "uploads", "students");
        Directory.CreateDirectory(uploadsRoot);
        var fileName = $"{userId:N}-avatar-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        using var stream = System.IO.File.Create(absolutePath);
        file.CopyTo(stream);

        return $"/uploads/students/{fileName}";
    }

    private static string ProfileCookieName(Guid userId, string field) => $"skillbridge_student_profile_{userId:N}_{field}";
}
