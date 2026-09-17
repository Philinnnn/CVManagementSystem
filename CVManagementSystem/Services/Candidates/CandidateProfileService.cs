namespace CVManagementSystem.Services.Candidates;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Candidates;
using Common;

public class CandidateProfileService(AppDbContext db) : ICandidateProfileService
{
    public async Task<ProfileDto?> GetProfileAsync(int candidateId)
    {
        var candidate = await db.Candidates
            .Include(c => c.AttributeValues)
            .ThenInclude(v => v.Attribute)
            .ThenInclude(a => a.SelectOptions)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate is null)
            return null;

        var all = candidate.AttributeValues.Select(ToDto).ToList();

        return new ProfileDto
        {
            CandidateId = candidate.Id,
            Version = candidate.Version,
            MeAttributes = all.Where(a => a.IsBuiltIn).ToList(),
            InfoAttributes = all.Where(a => !a.IsBuiltIn).ToList()
        };
    }

    public async Task<OperationResult> AddAttributeAsync(int candidateId, int attributeId)
    {
        var candidateExists = await db.Candidates.AnyAsync(c => c.Id == candidateId);
        if (!candidateExists)
            return OperationResult.Fail("Candidate not found");

        var attribute = await db.Attributes.FirstOrDefaultAsync(a => a.Id == attributeId);
        if (attribute is null)
            return OperationResult.Fail("Attribute not found");

        var alreadyAdded = await db.CandidateAttributeValues
            .AnyAsync(v => v.CandidateId == candidateId && v.AttributeId == attributeId);
        if (alreadyAdded)
            return OperationResult.Fail("Attribute is already in the profile");

        db.CandidateAttributeValues.Add(new CandidateAttributeValue
        {
            CandidateId = candidateId,
            AttributeId = attributeId
        });
        await db.SaveChangesAsync();

        return OperationResult.Ok();
    }

    public async Task<OperationResult> RemoveAttributeAsync(int candidateId, int attributeId)
    {
        var value = await db.CandidateAttributeValues
            .Include(v => v.Attribute)
            .FirstOrDefaultAsync(v => v.CandidateId == candidateId && v.AttributeId == attributeId);

        if (value is null)
            return OperationResult.Fail("Attribute is not in the profile");

        if (value.Attribute.IsBuiltIn)
            return OperationResult.Fail("Built-in attributes cannot be removed");

        db.CandidateAttributeValues.Remove(value);
        await db.SaveChangesAsync();

        return OperationResult.Ok();
    }

    public async Task<OperationResult<int>> SaveAsync(int candidateId, SaveProfileRequest request)
    {
        var candidate = await db.Candidates
            .Include(c => c.AttributeValues)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate is null)
            return OperationResult<int>.Fail("Candidate not found");

        var valuesByAttributeId = candidate.AttributeValues.ToDictionary(v => v.AttributeId);

        foreach (var input in request.Values)
        {
            if (!valuesByAttributeId.TryGetValue(input.AttributeId, out var value))
                continue; // ignore values for attributes not added to the profile

            value.TextValue = input.TextValue;
            value.NumericValue = input.NumericValue;
            value.BooleanValue = input.BooleanValue;
            value.DateValue = input.DateValue.AsUtc();
            value.DateRangeStart = input.DateRangeStart.AsUtc();
            value.DateRangeEnd = input.DateRangeEnd.AsUtc();
        }

        candidate.Version++;
        db.Entry(candidate).Property(c => c.Version).OriginalValue = request.Version;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<int>.Conflict();
        }

        return OperationResult<int>.Ok(candidate.Version);
    }

    private static AttributeValueDto ToDto(CandidateAttributeValue value) => new()
    {
        AttributeId = value.AttributeId,
        AttributeName = value.Attribute.Name,
        DataType = value.Attribute.DataType,
        IsBuiltIn = value.Attribute.IsBuiltIn,
        SelectOptions = value.Attribute.SelectOptions.OrderBy(o => o.OrderIndex).Select(o => o.OptionValue).ToList(),
        TextValue = value.TextValue,
        NumericValue = value.NumericValue,
        DateValue = value.DateValue,
        BooleanValue = value.BooleanValue,
        DateRangeStart = value.DateRangeStart,
        DateRangeEnd = value.DateRangeEnd
    };
    
    public async Task<int?> GetCandidateIdByUserIdAsync(int userId)
    {
        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.UserId == userId);
        return candidate?.Id;
    }
}