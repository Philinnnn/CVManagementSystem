namespace CVManagementSystem.Models.Positions;
using Candidates;

public class PositionTag
{
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}