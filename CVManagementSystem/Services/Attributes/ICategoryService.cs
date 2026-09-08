namespace CVManagementSystem.Services.Attributes;

using Dtos;
using Common;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<OperationResult<CategoryDto>> CreateAsync(string name);
}