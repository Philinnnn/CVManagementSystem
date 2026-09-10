namespace CVManagementSystem.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Discussions;
using Services.Positions;

[Authorize]
public class DiscussionsController(IDiscussionService discussionService, IPositionService positionService) : Controller
{
    public async Task<IActionResult> Index(int positionId)
    {
        var position = await positionService.GetByIdAsync(positionId);
        if (position is null)
            return NotFound();

        var messages = await discussionService.GetMessagesAsync(positionId);

        ViewBag.PositionId = positionId;
        ViewBag.PositionName = position.Name;
        return View(messages);
    }
}