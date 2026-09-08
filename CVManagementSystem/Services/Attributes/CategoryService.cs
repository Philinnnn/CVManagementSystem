namespace CVManagementSystem.Services.Attributes;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Models.Attributes;
using Common;

public class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync() =>
        await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();

    public async Task<OperationResult<CategoryDto>> CreateAsync(string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult<CategoryDto>.Fail("Name is required");

        if (await db.Categories.AnyAsync(c => c.Name == name))
            return OperationResult<CategoryDto>.Fail("This category already exists");

        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return OperationResult<CategoryDto>.Ok(new CategoryDto { Id = category.Id, Name = category.Name });
    }
}