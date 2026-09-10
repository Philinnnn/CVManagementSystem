namespace CVManagementSystem.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Admin;

[Authorize(Roles = "Administrator")]
public class AdminController(IUserAdminService userAdminService) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Roles = await userAdminService.GetAllRoleNamesAsync();
        var users = await userAdminService.GetAllAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(int[] ids)
    {
        await userAdminService.SetBlockedAsync(ids, true);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(int[] ids)
    {
        await userAdminService.SetBlockedAsync(ids, false);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int[] ids)
    {
        var result = await userAdminService.DeleteAsync(ids);
        if (!result.Success)
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(int[] ids, string roleName)
    {
        await userAdminService.AssignRoleAsync(ids, roleName);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(int[] ids, string roleName)
    {
        await userAdminService.RemoveRoleAsync(ids, roleName);
        return RedirectToAction(nameof(Index));
    }
}