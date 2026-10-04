namespace CVManagementSystem.Controllers;

using System.Security.Claims;
using System.Text.Json;
using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Common;
using Services.Integrations;
using Services.Localization;

[Authorize]
public class SupportController(IDropboxService dropboxService, AppDbContext db, IAppLocalizer appLocalizer) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(string summary, string priority, string link, string? positionName)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var reportedBy = $"{User.Identity!.Name} ({string.Join(", ", roles)})";
        var reporterEmail = "";
        
        if (User.TryGetUserId(out var userId))
        {
            var reporterUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            reporterEmail = reporterUser?.Email ?? "";
        }
        
        var adminEmails = await db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Administrator"))
            .Select(u => u.Email)
            .ToListAsync();

        var ticket = new SupportTicketDto
        {
            Summary = summary,
            ReportedBy = reportedBy,
            ReporterEmail = reporterEmail,
            Position = positionName,
            Link = link,
            Priority = priority,
            AdminEmails = adminEmails
        };

        var json = JsonSerializer.Serialize(ticket, new JsonSerializerOptions { WriteIndented = true });
        var fileName = $"ticket-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}.json";

        try
        {
            await dropboxService.UploadJsonAsync(fileName, json);
            TempData["Success"] = appLocalizer["support.ticket.submitted"];
        }
        catch (Exception ex)
        {
            TempData["Error"] = string.Format(appLocalizer["support.ticket.failed"], ex.Message);
        }

        return Redirect(link);
    }
}