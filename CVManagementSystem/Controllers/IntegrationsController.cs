namespace CVManagementSystem.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Integrations;

[Authorize(Roles = "Administrator")]
public class IntegrationsController(IDropboxService dropboxService) : Controller
{
    public IActionResult Index()
    {
        ViewBag.DropboxConnected = dropboxService.IsConnected;
        return View();
    }

    public IActionResult ConnectDropbox()
    {
        var redirectUri = Url.Action(nameof(DropboxCallback), null, null, Request.Scheme)!;
        return Redirect(dropboxService.GetAuthorizeUrl(redirectUri));
    }

    [Route("integrations/dropbox/callback")]
    public async Task<IActionResult> DropboxCallback(string code)
    {
        var redirectUri = Url.Action(nameof(DropboxCallback), null, null, Request.Scheme)!;
        await dropboxService.ExchangeCodeAsync(code, redirectUri);
        return RedirectToAction(nameof(Index));
    }
}