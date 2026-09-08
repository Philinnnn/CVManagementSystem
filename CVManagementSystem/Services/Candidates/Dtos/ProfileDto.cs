namespace CVManagementSystem.Services.Candidates.Dtos;

public class ProfileDto
{
    public int CandidateId { get; set; }
    public int Version { get; set; }
    public List<AttributeValueDto> MeAttributes { get; set; } = [];
    public List<AttributeValueDto> InfoAttributes { get; set; } = [];
}