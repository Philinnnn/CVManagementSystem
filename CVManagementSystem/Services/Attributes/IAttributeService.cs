namespace CVManagementSystem.Services.Attributes;

using Dtos;
using Common;

public interface IAttributeService
{
    Task<List<AttributeDto>> SearchAsync(string? prefix, int? categoryId);
    Task<AttributeDto?> GetByIdAsync(int id);
    Task<OperationResult<AttributeDto>> CreateAsync(CreateAttributeRequest request);
    Task<OperationResult<AttributeDto>> UpdateAsync(UpdateAttributeRequest request);
    Task<OperationResult> DeleteAsync(int id);
}