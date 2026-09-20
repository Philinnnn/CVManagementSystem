namespace CVManagementSystem.Services.Cvs.Dtos;

using Models.Cvs;

public class CvListItemDto
{
    public int Id { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public CvStatus Status { get; set; }
    public int LikeCount { get; set; }
    public int CandidateId { get; set; }
}