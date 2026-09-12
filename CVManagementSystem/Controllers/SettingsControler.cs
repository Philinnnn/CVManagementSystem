namespace CVManagementSystem.Controllers;

using System.Security.Claims;
using Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class SettingsController(AppDbContext db) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTheme(string theme, string returnUrl)
    {
        theme = theme == "dark" ? "dark" : "light";

        Response.Cookies.Append("theme", theme, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        await PersistIfLoggedInAsync(user => user.PreferredTheme = theme);

        return LocalRedirect(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLanguage(string culture, string returnUrl)
    {
        culture = culture is "en" or "ru" ? culture : "en";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        await PersistIfLoggedInAsync(user => user.PreferredLanguage = culture);

        return LocalRedirect(returnUrl);
    }

    private async Task PersistIfLoggedInAsync(Action<Models.Identity.User> apply)
    {
        if (User.Identity?.IsAuthenticated != true)
            return;

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return;

        apply(user);
        await db.SaveChangesAsync();
    }
}