namespace CVManagementSystem.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Discussions;

[Authorize]
public class DiscussionsController(IDiscussionService discussionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Messages(int positionId)
    {
        var messages = await discussionService.GetMessagesAsync(positionId);
        return Json(messages);
    }
}