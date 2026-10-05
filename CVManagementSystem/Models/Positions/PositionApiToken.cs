namespace CVManagementSystem.Models.Positions;

public class PositionApiToken
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}