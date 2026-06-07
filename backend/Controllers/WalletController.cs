using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class WalletController(SkillBridgeService service) : Controller
{
    [HttpGet]
    public IActionResult Deposit()
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Student);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = service.UserById(userId.Value);
        return View(new WalletCheckoutViewModel(
            userId.Value,
            UserRole.Student,
            user?.Name ?? "Học sinh",
            service.StudentWalletBalance(userId.Value)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DepositPreview(decimal amount, string method = "Visa / Mastercard")
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Student);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = service.UserById(userId.Value);
        if (amount <= 0)
        {
            return View("Deposit", new WalletCheckoutViewModel(userId.Value, UserRole.Student, user?.Name ?? "Học sinh", service.StudentWalletBalance(userId.Value), amount, method, "", "", false, null, "Số tiền nạp phải lớn hơn 0."));
        }

        return View("Deposit", new WalletCheckoutViewModel(
            userId.Value,
            UserRole.Student,
            user?.Name ?? "Học sinh",
            service.StudentWalletBalance(userId.Value),
            amount,
            method,
            $"DEP-TEMP-{DateTimeOffset.UtcNow:HHmmss}",
            "Chờ xác nhận",
            true));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmDeposit(decimal amount, string method = "Visa / Mastercard")
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Student);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.TopUpWallet(userId.Value, amount, method);
            TempData["Success"] = "Nạp tiền thành công.";
            return Redirect($"{Url.Action("Index", "Dashboard")}#payments");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            var user = service.UserById(userId.Value);
            return View("Deposit", new WalletCheckoutViewModel(userId.Value, UserRole.Student, user?.Name ?? "Học sinh", service.StudentWalletBalance(userId.Value), amount, method, "", "", false, null, ex.Message));
        }
    }

    [HttpGet]
    public IActionResult Withdraw()
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Teacher);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = service.UserById(userId.Value);
        var profile = service.TeacherProfileForUser(userId.Value);
        return View(new WalletCheckoutViewModel(
            userId.Value,
            UserRole.Teacher,
            user?.Name ?? "Giáo viên",
            service.WithdrawableBalance(userId.Value),
            0,
            profile?.DefaultPayoutMethod ?? "Tài khoản ngân hàng"));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Withdraw(decimal amount, string method = "Tài khoản ngân hàng", string accountName = "", string accountNumber = "", string bankName = "")
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Teacher);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.RequestWithdrawal(userId.Value, amount, method, accountName, accountNumber, bankName);
            TempData["Success"] = "Đã tạo yêu cầu rút tiền.";
            return Redirect($"{Url.Action("Index", "TeacherDashboard")}#payments");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            var user = service.UserById(userId.Value);
            return View(new WalletCheckoutViewModel(userId.Value, UserRole.Teacher, user?.Name ?? "Giáo viên", service.WithdrawableBalance(userId.Value), amount, method, "", "", false, null, ex.Message));
        }
    }
}
