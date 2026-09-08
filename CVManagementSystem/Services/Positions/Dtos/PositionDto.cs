namespace CVManagementSystem.Services.Positions.Dtos;

public class PositionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int Version { get; set; }
    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Open { get; set; }
    public int MaxProjects { get; set; }
    public List<string> ProjectTags { get; set; } = [];
    public List<PositionAttributeDto> Attributes { get; set; } = [];
    public List<AccessRuleDto> AccessRules { get; set; } = [];
    public int CvCount { get; set; }
}