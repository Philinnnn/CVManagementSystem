namespace CVManagementSystem.Services.Candidates.Dtos;

public class SaveProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Tags { get; set; } = [];
}