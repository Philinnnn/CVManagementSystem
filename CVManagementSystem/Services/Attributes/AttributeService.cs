namespace CVManagementSystem.Services.Attributes;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Attributes;
using Common;

public class AttributeService(AppDbContext db) : IAttributeService
{
    public async Task<List<AttributeDto>> SearchAsync(string? prefix, int? categoryId)
    {
        var query = db.Attributes
            .Include(a => a.Category)
            .Include(a => a.SelectOptions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(prefix))
            query = query.Where(a => EF.Functions.ILike(a.Name, prefix.Trim() + "%"));

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        var attributes = await query.OrderBy(a => a.Name).ToListAsync();
        return attributes.Select(ToDto).ToList();
    }

    public async Task<AttributeDto?> GetByIdAsync(int id)
    {
        var attribute = await db.Attributes
            .Include(a => a.Category)
            .Include(a => a.SelectOptions)
            .FirstOrDefaultAsync(a => a.Id == id);

        return attribute is null ? null : ToDto(attribute);
    }

    public async Task<OperationResult<AttributeDto>> CreateAsync(CreateAttributeRequest request)
    {
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return OperationResult<AttributeDto>.Fail("Name is required");

        if (!AttributeDataTypes.IsValid(request.DataType))
            return OperationResult<AttributeDto>.Fail("Unknown data type");

        if (await db.Attributes.AnyAsync(a => a.Name == name))
            return OperationResult<AttributeDto>.Fail("An attribute with this name already exists");

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return OperationResult<AttributeDto>.Fail("Category not found");

        if (request.DataType == AttributeDataTypes.Select && request.SelectOptions.Count == 0)
            return OperationResult<AttributeDto>.Fail("At least one option is required for a dropdown attribute");

        var attribute = new AttributeDefinition
        {
            Name = name,
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            DataType = request.DataType,
            Version = 1
        };

        if (request.DataType == AttributeDataTypes.Select)
        {
            attribute.SelectOptions = request.SelectOptions
                .Select((value, index) => new AttributeSelectOption { OptionValue = value.Trim(), OrderIndex = index })
                .ToList();
        }

        db.Attributes.Add(attribute);
        await db.SaveChangesAsync();

        var saved = await db.Attributes
            .Include(a => a.Category)
            .Include(a => a.SelectOptions)
            .FirstAsync(a => a.Id == attribute.Id);

        return OperationResult<AttributeDto>.Ok(ToDto(saved));
    }

    public async Task<OperationResult<AttributeDto>> UpdateAsync(UpdateAttributeRequest request)
    {
        var attribute = await db.Attributes
            .Include(a => a.SelectOptions)
            .FirstOrDefaultAsync(a => a.Id == request.Id);

        if (attribute is null)
            return OperationResult<AttributeDto>.Fail("Attribute not found");

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult<AttributeDto>.Fail("Name is required");

        if (await db.Attributes.AnyAsync(a => a.Name == name && a.Id != request.Id))
            return OperationResult<AttributeDto>.Fail("An attribute with this name already exists");

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return OperationResult<AttributeDto>.Fail("Category not found");

        attribute.Name = name;
        attribute.Description = request.Description.Trim();
        attribute.CategoryId = request.CategoryId;
        attribute.Version++;

        if (attribute.DataType == AttributeDataTypes.Select)
        {
            db.AttributeSelectOptions.RemoveRange(attribute.SelectOptions);
            attribute.SelectOptions = request.SelectOptions
                .Select((value, index) => new AttributeSelectOption { OptionValue = value.Trim(), OrderIndex = index })
                .ToList();
        }
        
        db.Entry(attribute).Property(a => a.Version).OriginalValue = request.Version;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<AttributeDto>.Conflict();
        }

        var saved = await db.Attributes
            .Include(a => a.Category)
            .Include(a => a.SelectOptions)
            .FirstAsync(a => a.Id == attribute.Id);

        return OperationResult<AttributeDto>.Ok(ToDto(saved));
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var attribute = await db.Attributes.FirstOrDefaultAsync(a => a.Id == id);
        if (attribute is null)
            return OperationResult.Fail("Attribute not found");

        db.Attributes.Remove(attribute);
        await db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    private static AttributeDto ToDto(AttributeDefinition attribute) => new()
    {
        Id = attribute.Id,
        Name = attribute.Name,
        Description = attribute.Description,
        CategoryId = attribute.CategoryId,
        CategoryName = attribute.Category?.Name ?? string.Empty,
        DataType = attribute.DataType,
        Version = attribute.Version,
        SelectOptions = attribute.SelectOptions
            .OrderBy(o => o.OrderIndex)
            .Select(o => o.OptionValue)
            .ToList()
    };
}