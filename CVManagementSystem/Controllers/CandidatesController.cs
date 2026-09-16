namespace CVManagementSystem.Controllers;

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Attributes;
using Models.ViewModels;
using Services.Attributes;
using Services.Candidates;
using Services.Candidates.Dtos;
using Services.Cvs;

[Authorize]
public class CandidatesController(
    ICandidateProfileService profileService,
    IProjectService projectService,
    ICvService cvService,
    IAttributeService attributeService) : Controller
{
    public async Task<IActionResult> Index(int? candidateId, string tab = "me")
    {
        var (targetId, canEdit) = await ResolveTargetAsync(candidateId);
        if (targetId is null)
            return Forbid();

        var profile = await profileService.GetProfileAsync(targetId.Value);
        if (profile is null)
            return NotFound();

        var vm = new CandidateProfileViewModel
        {
            Profile = profile,
            Projects = await projectService.GetAllAsync(targetId.Value),
            Cvs = await cvService.GetForCandidateAsync(targetId.Value)
        };

        if (canEdit)
        {
            var allAttributes = await attributeService.SearchAsync(null, null);
            var alreadyAdded = profile.InfoAttributes.Select(a => a.AttributeId).ToHashSet();
            ViewBag.AvailableAttributes = allAttributes.Where(a => !a.IsBuiltIn && !alreadyAdded.Contains(a.Id)).ToList();
        }

        ViewBag.CanEdit = canEdit;
        ViewBag.CandidateId = targetId.Value;
        ViewBag.ActiveTab = tab;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(int candidateId, int expectedVersion, string tab)
    {
        if (!await CanEditAsync(candidateId))
            return Forbid();

        var profile = await profileService.GetProfileAsync(candidateId);
        if (profile is null)
            return NotFound();

        var values = profile.MeAttributes.Concat(profile.InfoAttributes)
            .Select(a => ParseAttributeValue(a, Request.Form))
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();

        var result = await profileService.SaveAsync(candidateId, new SaveProfileRequest
        {
            Version = expectedVersion,
            Values = values
        });

        if (!result.Success)
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { candidateId, tab });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAttribute(int candidateId, int attributeId)
    {
        if (!await CanEditAsync(candidateId))
            return Forbid();

        var result = await profileService.AddAttributeAsync(candidateId, attributeId);
        if (!result.Success)
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { candidateId, tab = "info" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAttributes(int candidateId, int[] attributeIds)
    {
        if (!await CanEditAsync(candidateId))
            return Forbid();

        foreach (var attributeId in attributeIds)
        {
            var result = await profileService.RemoveAttributeAsync(candidateId, attributeId);
            if (!result.Success)
                TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new { candidateId, tab = "info" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProject(int candidateId, SaveProjectRequest request)
    {
        if (!await CanEditAsync(candidateId))
            return Forbid();

        var result = await projectService.CreateAsync(candidateId, request);
        if (!result.Success)
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { candidateId, tab = "projects" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProject(int candidateId, int projectId, SaveProjectRequest request)
    {
        if (!await CanEditAsync(candidateId))
            return Forbid();

        var result = await projectService.UpdateAsync(candidateId, projectId, request);
        if (!result.Success)
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index), new { candidateId, tab = "projects" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(int candidateId, int projectId)
    {
        if (!await CanEditAsync(candidateId))
            return Forbid();

        await projectService.DeleteAsync(candidateId, projectId);
        return RedirectToAction(nameof(Index), new { candidateId, tab = "projects" });
    }

    [HttpGet]
    public async Task<IActionResult> SuggestTags(string prefix) =>
        Json(await projectService.SuggestTagsAsync(prefix ?? string.Empty));

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> CanEditAsync(int candidateId)
    {
        var (targetId, canEdit) = await ResolveTargetAsync(candidateId);
        return targetId == candidateId && canEdit;
    }

    private async Task<(int? TargetId, bool CanEdit)> ResolveTargetAsync(int? requestedCandidateId)
    {
        if (User.IsInRole("Administrator") && requestedCandidateId.HasValue)
            return (requestedCandidateId, true);

        var ownCandidateId = await profileService.GetCandidateIdByUserIdAsync(GetUserId());

        if (ownCandidateId.HasValue && (requestedCandidateId is null || requestedCandidateId == ownCandidateId))
            return (ownCandidateId, true);

        if (requestedCandidateId.HasValue && (User.IsInRole("Recruiter") || User.IsInRole("Administrator")))
            return (requestedCandidateId, false); // read-only view of someone else's profile

        return (null, false);
    }

    private static AttributeValueInput? ParseAttributeValue(AttributeValueDto attribute, IFormCollection form)
    {
        var key = $"attr_{attribute.AttributeId}";

        return attribute.DataType switch
        {
            AttributeDataTypes.String or AttributeDataTypes.Text or AttributeDataTypes.Select or AttributeDataTypes.Image =>
                form.ContainsKey(key)
                    ? new AttributeValueInput { AttributeId = attribute.AttributeId, TextValue = form[key] }
                    : null,

            AttributeDataTypes.Numeric =>
                double.TryParse(form[key], NumberStyles.Any, CultureInfo.InvariantCulture, out var num)
                    ? new AttributeValueInput { AttributeId = attribute.AttributeId, NumericValue = num }
                    : null,

            AttributeDataTypes.Date =>
                DateTime.TryParse(form[key], CultureInfo.InvariantCulture, out var date)
                    ? new AttributeValueInput { AttributeId = attribute.AttributeId, DateValue = date }
                    : null,

            AttributeDataTypes.Boolean =>
                new AttributeValueInput { AttributeId = attribute.AttributeId, BooleanValue = form.ContainsKey(key) },

            AttributeDataTypes.Period =>
                DateTime.TryParse(form[$"{key}_start"], CultureInfo.InvariantCulture, out var start) &&
                DateTime.TryParse(form[$"{key}_end"], CultureInfo.InvariantCulture, out var end)
                    ? new AttributeValueInput { AttributeId = attribute.AttributeId, DateRangeStart = start, DateRangeEnd = end }
                    : null,

            _ => null
        };
    }
}