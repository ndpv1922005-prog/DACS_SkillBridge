using Microsoft.AspNetCore.SignalR;
using SkillBridge.Api.DTOs;
using SkillBridge.Api.Services;

namespace SkillBridge.Api.Hubs;

public sealed class SkillBridgeHub(SkillBridgeService service) : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await Clients.Others.SendAsync("UserOnline", userId);
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        var message = service.SaveMessage(request);
        await Clients.Group($"user:{request.ReceiverId}").SendAsync("MessageReceived", message);
        await Clients.Group($"user:{request.SenderId}").SendAsync("MessageReceived", message);
    }

    public async Task DeleteMessage(Guid messageId, Guid userId)
    {
        var message = service.DeleteMessage(messageId, userId);
        await Clients.Group($"user:{message.ReceiverId}").SendAsync("MessageDeleted", message);
        await Clients.Group($"user:{message.SenderId}").SendAsync("MessageDeleted", message);
    }

    public async Task Typing(Guid senderId, Guid receiverId)
    {
        await Clients.Group($"user:{receiverId}").SendAsync("Typing", senderId);
    }

    public async Task JoinCall(Guid bookingId, Guid userId)
    {
        if (!service.CanJoinCall(bookingId, userId))
        {
            await Clients.Caller.SendAsync("CallRejected", "Only paid bookings can join calls.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"call:{bookingId}");
        await Clients.Group($"call:{bookingId}").SendAsync("CallParticipantJoined", userId);
    }
}
