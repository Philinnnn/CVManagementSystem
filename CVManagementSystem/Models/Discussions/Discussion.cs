namespace CVManagementSystem.Models.Discussions;

using Positions;

public class Discussion
{
    public int Id { get; set; }

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DiscussionMessage> Messages { get; set; } = [];
}