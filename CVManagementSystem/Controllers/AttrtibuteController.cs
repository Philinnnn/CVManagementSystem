namespace CVManagementSystem.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Attributes;
using Services.Attributes;
using Services.Attributes.Dtos;

[Authorize(Roles = "Recruiter,Administrator")]
public class AttributesController(IAttributeService attributeService, ICategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index(string? q, int? categoryId)
    {
        ViewBag.Categories = await categoryService.GetAllAsync();
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.Query = q;
        var attributes = await attributeService.SearchAsync(q, categoryId);
        return View(attributes);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await categoryService.GetAllAsync();
        ViewBag.DataTypes = AttributeDataTypes.All;
        return View(new CreateAttributeRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAttributeRequest request, string? selectOptionsRaw)
    {
        request.SelectOptions = SplitOptions(selectOptionsRaw);

        var result = await attributeService.CreateAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ViewBag.Categories = await categoryService.GetAllAsync();
            ViewBag.DataTypes = AttributeDataTypes.All;
            return View(request);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var attribute = await attributeService.GetByIdAsync(id);
        if (attribute is null)
            return NotFound();

        ViewBag.Categories = await categoryService.GetAllAsync();
        return View(attribute);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateAttributeRequest request, string? selectOptionsRaw)
    {
        request.SelectOptions = SplitOptions(selectOptionsRaw);

        var result = await attributeService.UpdateAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ViewBag.Categories = await categoryService.GetAllAsync();
            return View(result.Value ?? await attributeService.GetByIdAsync(request.Id));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int[] ids)
    {
        foreach (var id in ids)
            await attributeService.DeleteAsync(id);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name)
    {
        await categoryService.CreateAsync(name);
        return RedirectToAction(nameof(Create));
    }

    private static List<string> SplitOptions(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}