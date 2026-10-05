namespace CVManagementSystem.Controllers.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Positions;

[ApiController]
[Route("api/positions")]
[AllowAnonymous]
public class PositionsApiController(IPositionService positionService) : ControllerBase
{
    [HttpGet("{token}/aggregate")]
    public async Task<IActionResult> GetAggregate(string token)
    {
        var result = await positionService.GetAggregateByTokenAsync(token);
        return result is null ? NotFound() : Ok(result);
    }
}