namespace CVManagementSystem.Controllers;

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Attributes;
using Models.Cvs;
using Services.Candidates;
using Services.Cvs;
using Services.Cvs.Dtos;

[Authorize]
public class CvsController(
    ICvService cvService,
    ICandidateProfileService candidateProfileService) : Controller
{
    [HttpPost]
    [Authorize(Roles = "Candidate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int positionId)
    {
        var candidateId = await GetCandidateIdAsync();
        if (candidateId is null)
            return Forbid();

        var result = await cvService.CreateAsync(candidateId.Value, positionId);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Details", "Positions", new { id = positionId });
        }

        return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = GetUserId();
        var cv = await cvService.GetByIdAsync(id, userId);
        if (cv is null)
            return NotFound();

        var candidateId = await candidateProfileService.GetCandidateIdByUserIdAsync(userId);
        var isOwner = User.IsInRole("Candidate") && candidateId == cv.CandidateId;

        if (!isOwner && !User.IsInRole("Administrator"))
        {
            var recruiterCanSee = User.IsInRole("Recruiter") && cv.Status == CvStatus.Published && cv.CandidateHasAccess;
            if (!recruiterCanSee)
                return Forbid();
        }

        ViewBag.CanEdit = isOwner || User.IsInRole("Administrator");
        ViewBag.IsRecruiter = User.IsInRole("Recruiter");
        return View(cv);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int expectedVersion)
    {
        var cv = await cvService.GetByIdAsync(id, GetUserId());
        if (cv is null)
            return NotFound();

        var currentVersion = expectedVersion;

        foreach (var attribute in cv.Attributes)
        {
            var request = ParseAttributeValue(attribute, Request.Form);
            if (request is null)
                continue;

            var result = await cvService.UpdateAttributeAsync(id, currentVersion, request);
            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            currentVersion = result.Value!.Version;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, int expectedVersion)
    {
        var result = await cvService.PublishAsync(id, expectedVersion);
        if (!result.Success)
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id)
    {
        await cvService.LikeAsync(id, GetUserId());
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlike(int id)
    {
        await cvService.UnlikeAsync(id, GetUserId());
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Candidate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var candidateId = await GetCandidateIdAsync();
        if (candidateId is null)
            return Forbid();

        await cvService.DeleteAsync(candidateId.Value, id);
        // TODO: redirect to a proper "My CVs" list once the Candidates controller exists
        return RedirectToAction("Index", "Home");
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<int?> GetCandidateIdAsync() => await candidateProfileService.GetCandidateIdByUserIdAsync(GetUserId());

    private static UpdateCvAttributeRequest? ParseAttributeValue(CvAttributeValueDto attribute, IFormCollection form)
    {
        var key = $"attr_{attribute.AttributeId}";

        return attribute.DataType switch
        {
            AttributeDataTypes.String or AttributeDataTypes.Text or AttributeDataTypes.Select or AttributeDataTypes.Image =>
                form.ContainsKey(key)
                    ? new UpdateCvAttributeRequest { AttributeId = attribute.AttributeId, TextValue = form[key] }
                    : null,

            AttributeDataTypes.Numeric =>
                double.TryParse(form[key], NumberStyles.Any, CultureInfo.InvariantCulture, out var num)
                    ? new UpdateCvAttributeRequest { AttributeId = attribute.AttributeId, NumericValue = num }
                    : null,

            AttributeDataTypes.Date =>
                DateTime.TryParse(form[key], CultureInfo.InvariantCulture, out var date)
                    ? new UpdateCvAttributeRequest { AttributeId = attribute.AttributeId, DateValue = date }
                    : null,

            AttributeDataTypes.Boolean =>
                new UpdateCvAttributeRequest { AttributeId = attribute.AttributeId, BooleanValue = form.ContainsKey(key) },

            AttributeDataTypes.Period =>
                DateTime.TryParse(form[$"{key}_start"], CultureInfo.InvariantCulture, out var start) &&
                DateTime.TryParse(form[$"{key}_end"], CultureInfo.InvariantCulture, out var end)
                    ? new UpdateCvAttributeRequest { AttributeId = attribute.AttributeId, DateRangeStart = start, DateRangeEnd = end }
                    : null,

            _ => null
        };
    }
}