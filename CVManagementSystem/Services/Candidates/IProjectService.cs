namespace CVManagementSystem.Services.Candidates;

using Dtos;
using Common;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllAsync(int candidateId);
    Task<OperationResult<ProjectDto>> CreateAsync(int candidateId, SaveProjectRequest request);
    Task<OperationResult<ProjectDto>> UpdateAsync(int candidateId, int projectId, SaveProjectRequest request);
    Task<OperationResult> DeleteAsync(int candidateId, int projectId);
    Task<List<string>> SuggestTagsAsync(string prefix);
}