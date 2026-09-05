namespace CVManagementSystem.Models.Candidates;
using Positions;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ProjectTag> ProjectTags { get; set; } = [];
    public ICollection<PositionTag> PositionTags { get; set; } = [];
}