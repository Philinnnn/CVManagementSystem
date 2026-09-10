namespace CVManagementSystem.Services.Candidates;

using Dtos;
using Common;

public interface ICandidateProfileService
{
    Task<ProfileDto?> GetProfileAsync(int candidateId);
    Task<OperationResult> AddAttributeAsync(int candidateId, int attributeId);
    Task<OperationResult> RemoveAttributeAsync(int candidateId, int attributeId);
    Task<OperationResult<int>> SaveAsync(int candidateId, SaveProfileRequest request);
    Task<int?> GetCandidateIdByUserIdAsync(int userId);
}