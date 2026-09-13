namespace CVManagementSystem.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Search;

[Authorize]
public class SearchController(ISearchService searchService) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var results = await searchService.SearchAsync(q);
        ViewBag.Query = q;
        return View(results);
    }
}