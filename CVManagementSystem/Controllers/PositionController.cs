using CVManagementSystem.Services.Common;

namespace CVManagementSystem.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Positions;
using Models.ViewModels;
using Services.Attributes;
using Services.Candidates;
using Services.Cvs;
using Services.Positions;
using Services.Positions.Dtos;

public class PositionsController(
    IPositionService positionService,
    IAttributeService attributeService,
    ICvService cvService,
    ICandidateProfileService candidateProfileService) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var positions = await positionService.GetAllAsync();
        return View(positions);
    }

    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<IActionResult> Create()
    {
        ViewBag.AllAttributes = await attributeService.SearchAsync(null, null);
        ViewBag.Operators = AccessRuleOperators.All;
        return View(new PositionFormViewModel { Open = true, MaxProjects = 5 });
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PositionFormViewModel vm)
    {
        var result = await positionService.CreateAsync(GetUserId(), ToCreateRequest(vm));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ViewBag.AllAttributes = await attributeService.SearchAsync(null, null);
            ViewBag.Operators = AccessRuleOperators.All;
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Recruiter,Administrator")]
    public async Task<IActionResult> Edit(int id)
    {
        var position = await positionService.GetByIdAsync(id);
        if (position is null)
            return NotFound();

        ViewBag.AllAttributes = await attributeService.SearchAsync(null, null);
        ViewBag.Operators = AccessRuleOperators.All;

        var vm = new PositionFormViewModel
        {
            Id = position.Id,
            Version = position.Version,
            Name = position.Name,
            ShortDescription = position.ShortDescription,
            MaxProjects = position.MaxProjects,
            Open = position.Open,
            ProjectTagsRaw = string.Join(", ", position.ProjectTags),
            SelectedAttributeIds = position.Attributes.Select(a => a.AttributeId).ToArray(),
            RequiredAttributeIds = position.Attributes.Where(a => a.Required).Select(a => a.AttributeId).ToArray(),
            RuleAttributeIds = position.AccessRules.Select(r => r.AttributeId).ToArray(),
            RuleOperators = position.AccessRules.Select(r => r.Operator).ToArray(),
            RuleExpectedValues = position.AccessRules.Select(r => r.ExpectedValue).ToArray()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PositionFormViewModel vm)
    {
        var request = new UpdatePositionRequest
        {
            Id = vm.Id,
            Version = vm.Version,
            Open = vm.Open,
            Name = vm.Name,
            ShortDescription = vm.ShortDescription,
            MaxProjects = vm.MaxProjects,
            ProjectTags = SplitTags(vm.ProjectTagsRaw),
            Attributes = vm.SelectedAttributeIds.Select(id => new PositionAttributeInput
            {
                AttributeId = id,
                Required = vm.RequiredAttributeIds.Contains(id)
            }).ToList(),
            AccessRules = BuildAccessRules(vm)
        };

        var result = await positionService.UpdateAsync(request);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ViewBag.AllAttributes = await attributeService.SearchAsync(null, null);
            ViewBag.Operators = AccessRuleOperators.All;
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int[] ids)
    {
        foreach (var id in ids)
            await positionService.DeleteAsync(id);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter,Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(int[] ids)
    {
        if (ids.Length == 1)
            await positionService.DuplicateAsync(ids[0], GetUserId());

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var position = await positionService.GetByIdAsync(id);
        if (position is null)
            return NotFound();

        ViewBag.Cvs = await cvService.GetPublishedForPositionAsync(id);

        if (User.IsInRole("Candidate"))
        {
            var candidateId = await candidateProfileService.GetCandidateIdByUserIdAsync(GetUserId());
            if (candidateId.HasValue)
            {
                ViewBag.CandidateHasAccess = await positionService.CandidateCanAccessAsync(id, candidateId.Value);
                ViewBag.CandidateAlreadyApplied = (await cvService.GetForCandidateAsync(candidateId.Value))
                    .Any(c => c.PositionName == position.Name);
            }
        }

        return View(position);
    }

    private int GetUserId()
    {
        if (!User.TryGetUserId(out var id))
            throw new InvalidOperationException("Session claims are invalid — please log out and log in again.");

        return id;
    }

    private static List<string> SplitTags(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static List<AccessRuleInput> BuildAccessRules(PositionFormViewModel vm)
    {
        var rules = new List<AccessRuleInput>();
        for (var i = 0; i < vm.RuleAttributeIds.Length; i++)
        {
            if (vm.RuleAttributeIds[i] <= 0) continue;
            if (i >= vm.RuleOperators.Length || i >= vm.RuleExpectedValues.Length) continue;
            if (string.IsNullOrWhiteSpace(vm.RuleExpectedValues[i])) continue;

            rules.Add(new AccessRuleInput
            {
                AttributeId = vm.RuleAttributeIds[i],
                Operator = vm.RuleOperators[i],
                ExpectedValue = vm.RuleExpectedValues[i]
            });
        }
        return rules;
    }

    private static CreatePositionRequest ToCreateRequest(PositionFormViewModel vm) => new()
    {
        Name = vm.Name,
        ShortDescription = vm.ShortDescription,
        MaxProjects = vm.MaxProjects,
        ProjectTags = SplitTags(vm.ProjectTagsRaw),
        Attributes = vm.SelectedAttributeIds.Select(id => new PositionAttributeInput
        {
            AttributeId = id,
            Required = vm.RequiredAttributeIds.Contains(id)
        }).ToList(),
        AccessRules = BuildAccessRules(vm)
    };
}