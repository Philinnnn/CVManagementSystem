namespace CVManagementSystem.Models.Integrations;

public class DropboxCredential
{
    public int Id { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}