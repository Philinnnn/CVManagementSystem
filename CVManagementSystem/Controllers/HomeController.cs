using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CVManagementSystem.Models;
using CVManagementSystem.Services.Dashboard;

namespace CVManagementSystem.Controllers;

public class HomeController (IDashboardService dashboardService, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var dashboard = await dashboardService.GetDashboardAsync();
        return View(dashboard);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}