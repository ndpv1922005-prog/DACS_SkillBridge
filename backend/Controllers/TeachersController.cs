using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class TeachersController(SkillBridgeService service) : Controller
{
    public IActionResult Index(string? query, TeachingMode? mode)
    {
        var model = new TeacherListViewModel(service.SearchTeachers(query, mode), query, mode);
        return View(model);
    }

    public IActionResult Details(Guid id, string? error = null)
    {
        var teacher = service.SearchTeachers(null, null).FirstOrDefault(x => x.Id == id);
        if (teacher is null)
        {
            return NotFound();
        }

        var slots = service.BookingSlotsForTeacher(teacher.UserId)
            .Select(slot => new BookingSlotViewModel(slot.Start, slot.End, slot.IsBooked, slot.TeachingMode, slot.OfflineLocation, slot.LessonContent));
        return View(new TeacherDetailViewModel(teacher, slots, error));
    }
}
