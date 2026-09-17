namespace CVManagementSystem.Services.Candidates;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Candidates;
using Common;

public class ProjectService(AppDbContext db) : IProjectService
{
    public async Task<List<ProjectDto>> GetAllAsync(int candidateId) =>
        await db.Projects
            .Where(p => p.CandidateId == candidateId)
            .Include(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
            .OrderByDescending(p => p.StartDate)
            .Select(p => ToDto(p))
            .ToListAsync();

    public async Task<OperationResult<ProjectDto>> CreateAsync(int candidateId, SaveProjectRequest request)
    {
        var error = Validate(request);
        if (error is not null)
            return OperationResult<ProjectDto>.Fail(error);

        var project = new CandidateProject
        {
            CandidateId = candidateId,
            Name = request.Name.Trim(),
            Description = request.Description,
            StartDate = request.StartDate.AsUtc(),
            EndDate = request.EndDate.AsUtc()
        };

        await ApplyTagsAsync(project, request.Tags);

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var saved = await LoadAsync(candidateId, project.Id);
        return OperationResult<ProjectDto>.Ok(ToDto(saved!));
    }

    public async Task<OperationResult<ProjectDto>> UpdateAsync(int candidateId, int projectId, SaveProjectRequest request)
    {
        var project = await db.Projects
            .Include(p => p.ProjectTags)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.CandidateId == candidateId);

        if (project is null)
            return OperationResult<ProjectDto>.Fail("Project not found");

        var error = Validate(request);
        if (error is not null)
            return OperationResult<ProjectDto>.Fail(error);

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.StartDate = request.StartDate.AsUtc();
        project.EndDate = request.EndDate.AsUtc();

        db.ProjectTags.RemoveRange(project.ProjectTags);
        project.ProjectTags.Clear();
        await ApplyTagsAsync(project, request.Tags);

        await db.SaveChangesAsync();

        var saved = await LoadAsync(candidateId, project.Id);
        return OperationResult<ProjectDto>.Ok(ToDto(saved!));
    }

    public async Task<OperationResult> DeleteAsync(int candidateId, int projectId)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.CandidateId == candidateId);
        if (project is null)
            return OperationResult.Fail("Project not found");

        db.Projects.Remove(project);
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<List<string>> SuggestTagsAsync(string prefix) =>
        await db.Tags
            .Where(t => EF.Functions.ILike(t.Name, prefix.Trim() + "%"))
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .Take(10)
            .ToListAsync();

    private static string? Validate(SaveProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Name is required";

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate < request.StartDate)
            return "End date cannot be earlier than start date";

        return null;
    }

    private async Task ApplyTagsAsync(CandidateProject project, List<string> tagNames)
    {
        var names = tagNames.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (names.Count == 0)
            return;

        var existingTags = (await db.Tags.Where(t => names.Contains(t.Name)).ToListAsync())
            .ToDictionary(t => t.Name);

        foreach (var name in names)
        {
            if (!existingTags.TryGetValue(name, out var tag))
            {
                tag = new Tag { Name = name };
                db.Tags.Add(tag);
                existingTags[name] = tag;
            }

            project.ProjectTags.Add(new ProjectTag { Tag = tag });
        }
    }

    private async Task<CandidateProject?> LoadAsync(int candidateId, int projectId) =>
        await db.Projects
            .Include(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.CandidateId == candidateId);

    private static ProjectDto ToDto(CandidateProject project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        Tags = project.ProjectTags.Select(pt => pt.Tag.Name).ToList()
    };
}