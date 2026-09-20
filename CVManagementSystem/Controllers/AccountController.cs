using CVManagementSystem.Services.Common;
using CVManagementSystem.Services.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

namespace CVManagementSystem.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Identity;
using Models.ViewModels;
using Services.Auth;

public class AccountController(IAuthService authService) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, user) = await authService.ValidateCredentialsAsync(model.Email, model.Password);
        if (!success || user is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Login error");
            return View(model);
        }

        await SignInAsync(user);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, user) = await authService.RegisterAsync(
            model.Login, model.Email, model.Password, model.Fullname);

        if (!success || user is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Registration error");
            return View(model);
        }

        await SignInAsync(user);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(User user)
    {
        Response.Cookies.Append("theme", user.PreferredTheme,
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(user.PreferredLanguage)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new("Fullname", user.Fullname)
        };

        claims.AddRange(user.UserRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });
    }
    
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items["flow"] = "login";
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLoginCallback(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult LinkExternalLogin(string provider)
    {
        var redirectUrl = Url.Action(nameof(LinkExternalLoginCallback));
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items["flow"] = "link";
        properties.Items["userId"] = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [Authorize]
    public IActionResult LinkExternalLoginCallback() => RedirectToAction(nameof(Manage));

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Manage([FromServices] Data.AppDbContext db)
    {
        if (!User.TryGetUserId(out var userId))
            return RedirectToAction(nameof(Logout));

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var links = await db.ExternalLogins.Where(el => el.UserId == userId).Select(el => el.Provider).ToListAsync();

        ViewBag.LinkedProviders = links;
        ViewBag.HasPassword = user?.HasPassword ?? true;
        return View();
    }
    
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkExternalLogin(string provider, [FromServices] IExternalAuthService externalAuthService)
    {
        if (!User.TryGetUserId(out var userId))
            return RedirectToAction(nameof(Logout));

        var result = await externalAuthService.UnlinkAsync(userId, provider);
        if (!result.Success)
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Manage));
    }

    [HttpGet]
    [Authorize]
    public IActionResult SetPassword(bool fromSocialSignup, [FromServices] IAppLocalizer appLocalizer)
    {
        if (fromSocialSignup)
            TempData["Notice"] = appLocalizer["account.setPassword.needsPasswordNotice"];

        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(string newPassword, string confirmPassword, [FromServices] IAppLocalizer appLocalizer)
    {
        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, appLocalizer["account.setPassword.mismatch"]);
            return View();
        }

        if (!User.TryGetUserId(out var userId))
            return RedirectToAction(nameof(Logout));

        var (success, error) = await authService.SetPasswordAsync(userId, newPassword);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to set password");
            return View();
        }

        TempData["Success"] = appLocalizer["account.setPassword.success"];
        return RedirectToAction(nameof(Manage));
    }
}