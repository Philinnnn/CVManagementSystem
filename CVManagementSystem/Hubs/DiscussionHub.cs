namespace CVManagementSystem.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Services.Discussions;

[Authorize]
public class DiscussionHub(IDiscussionService discussionService) : Hub
{
    public async Task JoinPosition(int positionId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(positionId));

    public async Task LeavePosition(int positionId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(positionId));

    public async Task SendMessage(int positionId, string text)
    {
        var userId = int.Parse(Context.UserIdentifier ?? "0");
        if (userId == 0)
        {
            await Clients.Caller.SendAsync("Error", "You must be signed in to send messages.");
            return;
        }

        var result = await discussionService.PostMessageAsync(positionId, userId, text);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", result.Error);
            return;
        }

        await Clients.Group(GroupName(positionId)).SendAsync("ReceiveMessage", result.Value);
    }

    private static string GroupName(int positionId) => $"position-{positionId}";
}