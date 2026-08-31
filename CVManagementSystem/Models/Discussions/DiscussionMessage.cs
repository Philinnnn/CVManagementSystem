namespace CVManagementSystem.Models.Discussions;

using Identity;

public class DiscussionMessage
{
    public long Id { get; set; }

    public int DiscussionId { get; set; }
    public Discussion Discussion { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string MessageText { get; set; } = string.Empty;
    public DateTime SendAt { get; set; } = DateTime.UtcNow;
}