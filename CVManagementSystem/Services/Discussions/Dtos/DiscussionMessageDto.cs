namespace CVManagementSystem.Services.Discussions.Dtos;

public class DiscussionMessageDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string UserFullname { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime SendAt { get; set; }
}