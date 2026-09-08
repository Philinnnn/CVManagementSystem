namespace CVManagementSystem.Services.Positions.Dtos;

public class CreatePositionRequest
{
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int MaxProjects { get; set; }
    public List<string> ProjectTags { get; set; } = [];
    public List<PositionAttributeInput> Attributes { get; set; } = [];
    public List<AccessRuleInput> AccessRules { get; set; } = [];
}