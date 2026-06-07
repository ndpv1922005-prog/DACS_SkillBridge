using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class HomeController(SkillBridgeService service) : Controller
{
    public IActionResult Index()
    {
        var userId = AuthSession.UserId(HttpContext, UserRole.Student);
        var model = new HomeViewModel(
            service.SearchTeachers(null, null),
            userId is null ? [] : service.BookingsForUser(userId.Value),
            userId,
            AuthSession.UserName(HttpContext, UserRole.Student));

        return View(model);
    }
}
