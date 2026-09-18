namespace CVManagementSystem.Services.Cvs;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Candidates;
using Models.Cvs;
using Models.Positions;
using Services.Positions;
using Common;

public class CvService(AppDbContext db, IPositionService positionService) : ICvService
{
    public async Task<List<CvListItemDto>> GetForCandidateAsync(int candidateId) =>
        await db.Cvs
            .Where(c => c.CandidateId == candidateId)
            .Include(c => c.Position)
            .Include(c => c.Candidate).ThenInclude(cand => cand.User)
            .Select(c => new CvListItemDto
            {
                Id = c.Id,
                CandidateName = c.Candidate.User.Fullname,
                PositionName = c.Position.Name,
                Status = c.Status,
                LikeCount = c.Likes.Count
            })
            .ToListAsync();

    public async Task<List<CvListItemDto>> GetPublishedForPositionAsync(int positionId)
    {
        var position = await db.Positions
            .Include(p => p.AccessRules).ThenInclude(r => r.Attribute)
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position is null)
            return [];

        var cvs = await db.Cvs
            .Where(c => c.PositionId == positionId && c.Status == CvStatus.Published)
            .Include(c => c.Candidate).ThenInclude(cand => cand.User)
            .Include(c => c.Likes)
            .ToListAsync();

        if (cvs.Count == 0 || position.AccessRules.Count == 0)
        {
            return cvs.Select(ToListItem).ToList();
        }
        
        var candidateIds = cvs.Select(c => c.CandidateId).Distinct().ToList();
        var attributeIds = position.AccessRules.Select(r => r.AttributeId).ToList();

        var valuesByCandidate = (await db.CandidateAttributeValues
                .Where(v => candidateIds.Contains(v.CandidateId) && attributeIds.Contains(v.AttributeId))
                .ToListAsync())
            .GroupBy(v => v.CandidateId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(v => v.AttributeId));

        return cvs
            .Where(c =>
            {
                var values = valuesByCandidate.GetValueOrDefault(c.CandidateId) ?? [];
                return position.AccessRules.All(rule =>
                    PositionAccessEvaluator.CandidateMatchesRule(rule, values.GetValueOrDefault(rule.AttributeId)));
            })
            .Select(ToListItem)
            .ToList();
    }

    public async Task<CvDto?> GetByIdAsync(int cvId, int? viewingUserId)
    {
        var cv = await LoadAsync(cvId);
        return cv is null ? null : await ToDtoAsync(cv, viewingUserId);
    }

    public async Task<OperationResult<CvDto>> CreateAsync(int candidateId, int positionId)
    {
        var position = await db.Positions
            .Include(p => p.PositionAttributes).ThenInclude(pa => pa.Attribute)
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position is null)
            return OperationResult<CvDto>.Fail("Position not found");

        if (!position.Open)
            return OperationResult<CvDto>.Fail("This position is closed");

        if (!await positionService.CandidateCanAccessAsync(positionId, candidateId))
            return OperationResult<CvDto>.Fail("You don't have access to this position");

        if (await db.Cvs.AnyAsync(c => c.CandidateId == candidateId && c.PositionId == positionId))
            return OperationResult<CvDto>.Fail("You already have a CV for this position");

        var profileValues = await db.CandidateAttributeValues
            .Where(v => v.CandidateId == candidateId)
            .ToDictionaryAsync(v => v.AttributeId);

        var cv = new Cv { CandidateId = candidateId, PositionId = positionId, Status = CvStatus.Draft, Version = 1 };

        foreach (var pa in position.PositionAttributes)
        {
            profileValues.TryGetValue(pa.AttributeId, out var profileValue);

            cv.AttributeValues.Add(new CvAttributeValue
            {
                AttributeId = pa.AttributeId,
                TextValue = profileValue?.TextValue,
                NumericValue = profileValue?.NumericValue,
                DateValue = profileValue?.DateValue,
                BooleanValue = profileValue?.BooleanValue,
                DateRangeStart = profileValue?.DateRangeStart,
                DateRangeEnd = profileValue?.DateRangeEnd
            });
        }

        db.Cvs.Add(cv);
        await db.SaveChangesAsync();

        var saved = await LoadAsync(cv.Id);
        return OperationResult<CvDto>.Ok(await ToDtoAsync(saved!, null));
    }

    public async Task<OperationResult> DeleteAsync(int candidateId, int cvId)
    {
        var cv = await db.Cvs.FirstOrDefaultAsync(c => c.Id == cvId && c.CandidateId == candidateId);
        if (cv is null)
            return OperationResult.Fail("CV not found");

        db.Cvs.Remove(cv);
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult<CvDto>> UpdateAttributeAsync(int cvId, int expectedVersion, UpdateCvAttributeRequest request)
    {
        var cv = await db.Cvs
            .Include(c => c.AttributeValues)
            .FirstOrDefaultAsync(c => c.Id == cvId);

        if (cv is null)
            return OperationResult<CvDto>.Fail("CV not found");

        if (cv.Status == CvStatus.Published)
            return OperationResult<CvDto>.Fail("Published CVs cannot be edited directly. Unpublish it first.");

        var cvValue = cv.AttributeValues.FirstOrDefault(v => v.AttributeId == request.AttributeId);
        if (cvValue is null)
            return OperationResult<CvDto>.Fail("This attribute is not part of the position");

        cvValue.TextValue = request.TextValue;
        cvValue.NumericValue = request.NumericValue;
        cvValue.DateValue = request.DateValue.AsUtc();
        cvValue.BooleanValue = request.BooleanValue;
        cvValue.DateRangeStart = request.DateRangeStart.AsUtc();
        cvValue.DateRangeEnd = request.DateRangeEnd.AsUtc();
        
        var profileValue = await db.CandidateAttributeValues
            .FirstOrDefaultAsync(v => v.CandidateId == cv.CandidateId && v.AttributeId == request.AttributeId);

        if (profileValue is null)
        {
            profileValue = new CandidateAttributeValue { CandidateId = cv.CandidateId, AttributeId = request.AttributeId };
            db.CandidateAttributeValues.Add(profileValue);
        }

        profileValue.TextValue = request.TextValue;
        profileValue.NumericValue = request.NumericValue;
        profileValue.DateValue = request.DateValue.AsUtc();
        profileValue.BooleanValue = request.BooleanValue;
        profileValue.DateRangeStart = request.DateRangeStart.AsUtc();
        profileValue.DateRangeEnd = request.DateRangeEnd.AsUtc();
        
        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == cv.CandidateId);
        if (candidate is not null)
            candidate.Version++;

        cv.Version++;
        db.Entry(cv).Property(c => c.Version).OriginalValue = expectedVersion;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<CvDto>.Conflict();
        }

        var saved = await LoadAsync(cv.Id);
        return OperationResult<CvDto>.Ok(await ToDtoAsync(saved!, null));
    }

    public async Task<OperationResult<CvDto>> PublishAsync(int cvId, int expectedVersion)
    {
        var cv = await db.Cvs
            .Include(c => c.AttributeValues)
            .Include(c => c.Position).ThenInclude(p => p.PositionAttributes)
            .FirstOrDefaultAsync(c => c.Id == cvId);

        if (cv is null)
            return OperationResult<CvDto>.Fail("CV not found");

        var requiredAttributeIds = cv.Position.PositionAttributes
            .Where(pa => pa.Required)
            .Select(pa => pa.AttributeId)
            .ToHashSet();

        var hasEmptyRequired = cv.AttributeValues
            .Where(v => requiredAttributeIds.Contains(v.AttributeId))
            .Any(v => v.TextValue is null && v.NumericValue is null && v.DateValue is null &&
                      v.BooleanValue is null && v.DateRangeStart is null && v.DateRangeEnd is null);

        if (hasEmptyRequired)
            return OperationResult<CvDto>.Fail("Fill in all required attributes before publishing");

        cv.Status = CvStatus.Published;
        cv.Version++;
        db.Entry(cv).Property(c => c.Version).OriginalValue = expectedVersion;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<CvDto>.Conflict();
        }

        var saved = await LoadAsync(cv.Id);
        return OperationResult<CvDto>.Ok(await ToDtoAsync(saved!, null));
    }

    public async Task<OperationResult> LikeAsync(int cvId, int recruiterUserId)
    {
        if (!await db.Cvs.AnyAsync(c => c.Id == cvId))
            return OperationResult.Fail("CV not found");

        if (await db.CvLikes.AnyAsync(l => l.CvId == cvId && l.UserId == recruiterUserId))
            return OperationResult.Fail("Already liked");

        db.CvLikes.Add(new CvLike { CvId = cvId, UserId = recruiterUserId });
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> UnlikeAsync(int cvId, int recruiterUserId)
    {
        var like = await db.CvLikes.FirstOrDefaultAsync(l => l.CvId == cvId && l.UserId == recruiterUserId);
        if (like is null)
            return OperationResult.Fail("Like not found");

        db.CvLikes.Remove(like);
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    private static CvListItemDto ToListItem(Cv c) => new()
    {
        Id = c.Id,
        CandidateName = c.Candidate.User.Fullname,
        PositionName = c.Position.Name,
        Status = c.Status,
        LikeCount = c.Likes.Count
    };

    private static List<CvProjectDto> ComputeProjects(Candidate candidate, Position position)
    {
        var positionTagNames = position.PositionTags.Select(pt => pt.Tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        IEnumerable<CandidateProject> projects = candidate.Projects;
        if (positionTagNames.Count > 0)
            projects = projects.Where(p => p.ProjectTags.Any(pt => positionTagNames.Contains(pt.Tag.Name)));

        return projects
            .OrderByDescending(p => p.StartDate)
            .Take(position.MaxProjects > 0 ? position.MaxProjects : int.MaxValue)
            .Select(p => new CvProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Tags = p.ProjectTags.Select(pt => pt.Tag.Name).ToList()
            })
            .ToList();
    }

    private async Task<CvDto> ToDtoAsync(Cv cv, int? viewingUserId)
    {
        var hasAccess = await positionService.CandidateCanAccessAsync(cv.PositionId, cv.CandidateId);

        return new CvDto
        {
            Id = cv.Id,
            CandidateId = cv.CandidateId,
            CandidateName = cv.Candidate.User.Fullname,
            PositionId = cv.PositionId,
            PositionName = cv.Position.Name,
            Status = cv.Status,
            Version = cv.Version,
            LikeCount = cv.Likes.Count,
            LikedByCurrentUser = viewingUserId.HasValue && cv.Likes.Any(l => l.UserId == viewingUserId.Value),
            CandidateHasAccess = hasAccess,
            Attributes = cv.AttributeValues.Select(v => new CvAttributeValueDto
            {
                AttributeId = v.AttributeId,
                AttributeName = v.Attribute.Name,
                DataType = v.Attribute.DataType,
                Required = cv.Position.PositionAttributes.First(pa => pa.AttributeId == v.AttributeId).Required,
                SelectOptions = v.Attribute.SelectOptions.OrderBy(o => o.OrderIndex).Select(o => o.OptionValue).ToList(),
                TextValue = v.TextValue,
                NumericValue = v.NumericValue,
                DateValue = v.DateValue,
                BooleanValue = v.BooleanValue,
                DateRangeStart = v.DateRangeStart,
                DateRangeEnd = v.DateRangeEnd
            }).ToList(),
            Projects = ComputeProjects(cv.Candidate, cv.Position)
        };
    }

    private async Task<Cv?> LoadAsync(int cvId) =>
        await db.Cvs
            .Include(c => c.Candidate).ThenInclude(cand => cand.User)
            .Include(c => c.Candidate).ThenInclude(cand => cand.Projects).ThenInclude(p => p.ProjectTags).ThenInclude(pt => pt.Tag)
            .Include(c => c.Position).ThenInclude(p => p.PositionAttributes)
            .Include(c => c.Position).ThenInclude(p => p.PositionTags).ThenInclude(pt => pt.Tag)
            .Include(c => c.AttributeValues).ThenInclude(v => v.Attribute).ThenInclude(a => a.SelectOptions)
            .Include(c => c.Likes)
            .FirstOrDefaultAsync(c => c.Id == cvId);
}