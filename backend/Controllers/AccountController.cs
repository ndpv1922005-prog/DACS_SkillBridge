using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.DTOs;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class AccountController(SkillBridgeService service) : Controller
{
    [HttpGet]
    public IActionResult Login() =>
        View(new LoginViewModel("", ""));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        try
        {
            var user = service.Login(new LoginRequest(model.Email, model.Password));
            AuthSession.SignIn(Response, user);
            if (user.Role == Models.UserRole.Teacher)
            {
                return RedirectToAction("Index", "TeacherDashboard");
            }
            if (user.Role == Models.UserRole.Admin)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View(model with { Error = UiText.T(HttpContext, ex.Message) });
        }
    }

    [HttpGet]
    public IActionResult Register() =>
        View(new RegisterViewModel("", "", "", Models.UserRole.Student));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        try
        {
            var user = service.Register(new AuthRequest(model.Name, model.Email, model.Password, model.Role));
            AuthSession.SignIn(Response, user);
            if (user.Role == Models.UserRole.Teacher)
            {
                return RedirectToAction("Index", "TeacherDashboard");
            }
            if (user.Role == Models.UserRole.Admin)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return View(model with { Error = UiText.T(HttpContext, ex.Message) });
        }
    }

    [HttpGet("/forgot-password")]
    public IActionResult ForgotPassword() =>
        View(new ForgotPasswordViewModel(""));

    [HttpPost("/forgot-password")]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model) =>
        View(model with { Submitted = true });

    [HttpGet("/reset-password")]
    public IActionResult ResetPassword() =>
        View(new ResetPasswordViewModel("", ""));

    [HttpPost("/reset-password")]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            return View(model with { Error = UiText.T(HttpContext, "ERR_RESET_REQUIRED") });
        }

        if (model.NewPassword != model.ConfirmPassword)
        {
            return View(model with { Error = UiText.T(HttpContext, "ERR_CONFIRM_MISMATCH") });
        }

        return View(new ResetPasswordViewModel("", "", UiText.T(HttpContext, "MockResetSuccess")));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout(string? role)
    {
        var parsedRole = Enum.TryParse<Models.UserRole>(role, out var requestedRole)
            ? requestedRole
            : AuthSession.RoleFor(HttpContext);

        if (parsedRole is not null)
        {
            AuthSession.SignOut(Response, parsedRole.Value);
        }

        return RedirectToAction("Index", "Home");
    }
}
