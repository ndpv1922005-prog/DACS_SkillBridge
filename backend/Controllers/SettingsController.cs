using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class SettingsController(SkillBridgeService service) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (CurrentUserId() is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new ChangePasswordViewModel("", "", "", UiText.Lang(HttpContext)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ChangePasswordViewModel model)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            service.ChangePassword(userId.Value, model.CurrentPassword, model.NewPassword, model.ConfirmPassword);
            return View(new ChangePasswordViewModel("", "", "", UiText.Lang(HttpContext), UiText.T(HttpContext, "PasswordChanged")));
        }
        catch (InvalidOperationException ex)
        {
            return View(model with { Language = UiText.Lang(HttpContext), Error = UiText.T(HttpContext, ex.Message) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Language(string language)
    {
        var lang = language == "en" ? "en" : "vi";
        Response.Cookies.Append("skillbridge_lang", lang, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

        var returnUrl = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(new Uri(returnUrl).PathAndQuery))
        {
            return LocalRedirect(new Uri(returnUrl).PathAndQuery);
        }

        return RedirectToAction("Index", "Home");
    }

    private Guid? CurrentUserId() =>
        AuthSession.UserId(HttpContext, AuthSession.RoleFor(HttpContext) ?? UserRole.Student);
}
