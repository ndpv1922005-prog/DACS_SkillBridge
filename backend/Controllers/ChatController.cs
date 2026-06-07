using Microsoft.AspNetCore.Mvc;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Controllers;

public sealed class ChatController(SkillBridgeService service) : Controller
{
    public IActionResult Index(Guid? contactId = null)
    {
        var role = AuthSession.RoleFor(HttpContext) ?? UserRole.Student;
        var userId = AuthSession.UserId(HttpContext, role);
        if (userId is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var contacts = service.ChatContactsForUser(userId.Value, role).ToList();
        if (contactId is { } requestedContactId && requestedContactId != userId.Value && contacts.All(user => user.Id != requestedContactId))
        {
            var requestedContact = service.UserById(requestedContactId);
            if (requestedContact is not null && IsAllowedContact(role, requestedContact.Role))
            {
                contacts.Add(requestedContact);
            }
        }

        return View(new ChatViewModel(
            contacts
                .Select(contact => BuildContact(userId.Value, contact))
                .OrderByDescending(contact => contact.LastMessageAt ?? DateTimeOffset.MinValue)
                .ThenBy(contact => contact.Name),
            userId.Value,
            contactId));
    }

    public IActionResult Support()
    {
        var role = AuthSession.RoleFor(HttpContext);
        if (role is null || role == UserRole.Admin)
        {
            return RedirectToAction(nameof(Index), new { role });
        }

        var admin = service.DefaultAdminUser();
        return admin is null
            ? RedirectToAction(nameof(Index), new { role })
            : RedirectToAction(nameof(Index), new { contactId = admin.Id, role });
    }

    private ChatContactViewModel BuildContact(Guid currentUserId, User contact)
    {
        var lastMessage = service.ConversationForViewer(currentUserId, contact.Id).LastOrDefault();

        return new ChatContactViewModel(
            contact.Id,
            contact.Name,
            contact.Role switch
            {
                UserRole.Teacher => "Giáo viên",
                UserRole.Admin => "Hỗ trợ",
                _ => "Học viên"
            },
            contact.AvatarUrl,
            lastMessage is null ? "Chưa có tin nhắn" : lastMessage.Content,
            lastMessage?.CreatedAt,
            service.UnreadMessagesFromContact(currentUserId, contact.Id));
    }

    private static bool IsAllowedContact(UserRole currentRole, UserRole contactRole) =>
        (currentRole == UserRole.Student && contactRole == UserRole.Teacher) ||
        (currentRole == UserRole.Teacher && contactRole == UserRole.Student) ||
        (currentRole is UserRole.Student or UserRole.Teacher && contactRole == UserRole.Admin) ||
        (currentRole == UserRole.Admin && contactRole != UserRole.Admin);
}
