namespace CVManagementSystem.Models.Identity;

public class ExternalLogin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Provider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
}