namespace CVManagementSystem.Services.Positions;

using Dtos;
using Common;

public interface IPositionService
{
    Task<List<PositionListItemDto>> GetAllAsync();
    Task<PositionDto?> GetByIdAsync(int id);
    Task<OperationResult<PositionDto>> CreateAsync(int creatorId, CreatePositionRequest request);
    Task<OperationResult<PositionDto>> UpdateAsync(UpdatePositionRequest request);
    Task<OperationResult<PositionDto>> DuplicateAsync(int positionId, int creatorId);
    Task<OperationResult> DeleteAsync(int id);
    Task<bool> CandidateCanAccessAsync(int positionId, int candidateId);
    Task<List<PositionListItemDto>> SearchByTagAsync(string tag);
}