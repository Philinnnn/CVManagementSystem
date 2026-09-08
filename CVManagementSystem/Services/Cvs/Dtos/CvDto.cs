namespace CVManagementSystem.Services.Cvs.Dtos;

using Models.Cvs;

public class CvDto
{
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public CvStatus Status { get; set; }
    public int Version { get; set; }
    public int LikeCount { get; set; }
    public bool LikedByCurrentUser { get; set; }
    public bool CandidateHasAccess { get; set; }
    public List<CvAttributeValueDto> Attributes { get; set; } = [];
    public List<CvProjectDto> Projects { get; set; } = [];
}