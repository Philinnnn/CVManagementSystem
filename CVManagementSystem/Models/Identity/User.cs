namespace CVManagementSystem.Models.Identity;

using Candidates;
using Cvs;
using Discussions;
using Positions;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Passhash { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public bool HasPassword { get; set; } = true;
    public string PreferredLanguage { get; set; } = "en";
    public string PreferredTheme { get; set; } = "light";
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];

    public Candidate? Candidate { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<Position> CreatedPositions { get; set; } = [];
    public ICollection<CvLike> CvLikes { get; set; } = [];
    public ICollection<DiscussionMessage> DiscussionMessages { get; set; } = [];
    public NpgsqlTypes.NpgsqlTsVector SearchVector { get; set; } = null!;
    public string? SalesforceContactId { get; set; }
    public DateTime? SalesforceSyncedAt { get; set; }
}