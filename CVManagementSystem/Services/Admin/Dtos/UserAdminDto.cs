namespace CVManagementSystem.Services.Admin.Dtos;

public class UserAdminDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public List<string> Roles { get; set; } = [];
    public int? CandidateId { get; set; }
    public bool HasCreatedPositions { get; set; }
}