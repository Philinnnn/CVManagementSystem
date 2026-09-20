namespace CVManagementSystem.Services.Cvs;

using Dtos;
using Common;

public interface ICvService
{
    Task<List<CvListItemDto>> GetForCandidateAsync(int candidateId);
    Task<List<CvListItemDto>> GetPublishedForPositionAsync(int positionId);
    Task<CvDto?> GetByIdAsync(int cvId, int? viewingUserId);
    Task<OperationResult<CvDto>> CreateAsync(int candidateId, int positionId);
    Task<OperationResult> DeleteAsync(int candidateId, int cvId);
    Task<OperationResult<CvDto>> UpdateAttributeAsync(int cvId, int expectedVersion, UpdateCvAttributeRequest request);
    Task<OperationResult<CvDto>> PublishAsync(int cvId, int expectedVersion);
    Task<OperationResult> LikeAsync(int cvId, int recruiterUserId);
    Task<OperationResult> UnlikeAsync(int cvId, int recruiterUserId);
    Task<List<CvListItemDto>> GetPublishedByTagAsync(string tag);
}