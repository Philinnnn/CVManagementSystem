namespace CVManagementSystem.Services.Positions;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Attributes;
using Models.Candidates;
using Models.Positions;
using Common;

public class PositionService(AppDbContext db) : IPositionService
{
    public async Task<List<PositionListItemDto>> GetAllAsync() =>
        await db.Positions
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PositionListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                CreatedAt = p.CreatedAt,
                Open = p.Open,
                CvCount = p.Cvs.Count
            })
            .ToListAsync();

    public async Task<PositionDto?> GetByIdAsync(int id)
    {
        var position = await LoadFullAsync(id);
        return position is null ? null : ToDto(position);
    }

    public async Task<OperationResult<PositionDto>> CreateAsync(int creatorId, CreatePositionRequest request)
    {
        var error = await ValidateAsync(request, null);
        if (error is not null)
            return OperationResult<PositionDto>.Fail(error);

        var position = new Position
        {
            Name = request.Name.Trim(),
            ShortDescription = request.ShortDescription.Trim(),
            MaxProjects = request.MaxProjects,
            CreatorId = creatorId,
            Version = 1
        };

        ApplyAttributes(position, request.Attributes);
        ApplyAccessRules(position, request.AccessRules);
        await ApplyTagsAsync(position, request.ProjectTags);

        db.Positions.Add(position);
        await db.SaveChangesAsync();

        var saved = await LoadFullAsync(position.Id);
        return OperationResult<PositionDto>.Ok(ToDto(saved!));
    }

    public async Task<OperationResult<PositionDto>> UpdateAsync(UpdatePositionRequest request)
    {
        var position = await db.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.AccessRules)
            .Include(p => p.PositionTags)
            .FirstOrDefaultAsync(p => p.Id == request.Id);

        if (position is null)
            return OperationResult<PositionDto>.Fail("Position not found");

        var error = await ValidateAsync(request, request.Id);
        if (error is not null)
            return OperationResult<PositionDto>.Fail(error);

        position.Name = request.Name.Trim();
        position.ShortDescription = request.ShortDescription.Trim();
        position.MaxProjects = request.MaxProjects;
        position.Open = request.Open;
        position.Version++;

        db.PositionAttributes.RemoveRange(position.PositionAttributes);
        position.PositionAttributes.Clear();
        ApplyAttributes(position, request.Attributes);

        db.PositionAccessRules.RemoveRange(position.AccessRules);
        position.AccessRules.Clear();
        ApplyAccessRules(position, request.AccessRules);

        db.PositionTags.RemoveRange(position.PositionTags);
        position.PositionTags.Clear();
        await ApplyTagsAsync(position, request.ProjectTags);

        db.Entry(position).Property(p => p.Version).OriginalValue = request.Version;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<PositionDto>.Conflict();
        }

        var saved = await LoadFullAsync(position.Id);
        return OperationResult<PositionDto>.Ok(ToDto(saved!));
    }

    public async Task<OperationResult<PositionDto>> DuplicateAsync(int positionId, int creatorId)
    {
        var source = await LoadFullAsync(positionId);
        if (source is null)
            return OperationResult<PositionDto>.Fail("Position not found");

        var copy = new Position
        {
            Name = source.Name + " (Copy)",
            ShortDescription = source.ShortDescription,
            MaxProjects = source.MaxProjects,
            CreatorId = creatorId,
            Version = 1,
            Open = true,
            PositionAttributes = source.PositionAttributes
                .Select(pa => new PositionAttribute { AttributeId = pa.AttributeId, Required = pa.Required })
                .ToList(),
            AccessRules = source.AccessRules
                .Select(r => new PositionAccessRule { AttributeId = r.AttributeId, Operator = r.Operator, ExpectedValue = r.ExpectedValue })
                .ToList(),
            PositionTags = source.PositionTags
                .Select(pt => new PositionTag { TagId = pt.TagId })
                .ToList()
        };

        db.Positions.Add(copy);
        await db.SaveChangesAsync();

        var saved = await LoadFullAsync(copy.Id);
        return OperationResult<PositionDto>.Ok(ToDto(saved!));
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var position = await db.Positions.FirstOrDefaultAsync(p => p.Id == id);
        if (position is null)
            return OperationResult.Fail("Position not found");

        db.Positions.Remove(position);
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<bool> CandidateCanAccessAsync(int positionId, int candidateId)
    {
        var position = await db.Positions
            .Include(p => p.AccessRules)
            .ThenInclude(r => r.Attribute)
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position is null)
            return false;

        if (position.AccessRules.Count == 0)
            return true; // no rules = public position

        var attributeIds = position.AccessRules.Select(r => r.AttributeId).ToList();
        var values = await db.CandidateAttributeValues
            .Where(v => v.CandidateId == candidateId && attributeIds.Contains(v.AttributeId))
            .ToDictionaryAsync(v => v.AttributeId);

        return position.AccessRules.All(rule =>
            PositionAccessEvaluator.CandidateMatchesRule(rule, values.GetValueOrDefault(rule.AttributeId)));
    }

    private async Task<string?> ValidateAsync(CreatePositionRequest request, int? excludingId)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required";

        var duplicateQuery = db.Positions.Where(p => p.Name == name);
        if (excludingId.HasValue)
            duplicateQuery = duplicateQuery.Where(p => p.Id != excludingId.Value);
        if (await duplicateQuery.AnyAsync())
            return "A position with this name already exists";

        if (request.MaxProjects < 0)
            return "Max projects cannot be negative";

        var attributeIds = request.Attributes.Select(a => a.AttributeId).ToList();
        if (attributeIds.Count != attributeIds.Distinct().Count())
            return "Duplicate attributes in the position";

        var existingCount = await db.Attributes.CountAsync(a => attributeIds.Contains(a.Id));
        if (existingCount != attributeIds.Count)
            return "One or more attributes do not exist";

        foreach (var rule in request.AccessRules)
        {
            if (!AccessRuleOperators.IsValid(rule.Operator))
                return $"Invalid operator '{rule.Operator}'";

            var attribute = await db.Attributes.FirstOrDefaultAsync(a => a.Id == rule.AttributeId);
            if (attribute is null)
                return "Access rule references an attribute that does not exist";

            if (attribute.DataType is AttributeDataTypes.Text or AttributeDataTypes.Image or AttributeDataTypes.Period)
                return $"Attribute '{attribute.Name}' cannot be used in an access rule";
        }

        return null;
    }

    private static void ApplyAttributes(Position position, List<PositionAttributeInput> attributes)
    {
        foreach (var input in attributes)
        {
            position.PositionAttributes.Add(new PositionAttribute
            {
                AttributeId = input.AttributeId,
                Required = input.Required
            });
        }
    }

    private static void ApplyAccessRules(Position position, List<AccessRuleInput> rules)
    {
        foreach (var input in rules)
        {
            position.AccessRules.Add(new PositionAccessRule
            {
                AttributeId = input.AttributeId,
                Operator = input.Operator,
                ExpectedValue = input.ExpectedValue.Trim()
            });
        }
    }

    private async Task ApplyTagsAsync(Position position, List<string> tagNames)
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

            position.PositionTags.Add(new PositionTag { Tag = tag });
        }
    }

    private async Task<Position?> LoadFullAsync(int id) =>
        await db.Positions
            .Include(p => p.Creator)
            .Include(p => p.PositionAttributes).ThenInclude(pa => pa.Attribute)
            .Include(p => p.AccessRules).ThenInclude(r => r.Attribute)
            .Include(p => p.PositionTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Cvs)
            .FirstOrDefaultAsync(p => p.Id == id);

    private static PositionDto ToDto(Position position) => new()
    {
        Id = position.Id,
        Name = position.Name,
        ShortDescription = position.ShortDescription,
        Version = position.Version,
        CreatorId = position.CreatorId,
        CreatorName = position.Creator?.Fullname ?? string.Empty,
        CreatedAt = position.CreatedAt,
        Open = position.Open,
        MaxProjects = position.MaxProjects,
        ProjectTags = position.PositionTags.Select(pt => pt.Tag.Name).ToList(),
        Attributes = position.PositionAttributes.Select(pa => new PositionAttributeDto
        {
            AttributeId = pa.AttributeId,
            AttributeName = pa.Attribute.Name,
            DataType = pa.Attribute.DataType,
            Required = pa.Required
        }).ToList(),
        AccessRules = position.AccessRules.Select(r => new AccessRuleDto
        {
            Id = r.Id,
            AttributeId = r.AttributeId,
            AttributeName = r.Attribute.Name,
            Operator = r.Operator,
            ExpectedValue = r.ExpectedValue
        }).ToList(),
        CvCount = position.Cvs.Count
    };
}