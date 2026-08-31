using CVManagementSystem.Models.Candidates;

namespace CVManagementSystem.Models.Positions;

using Cvs;
using Discussions;
using Identity;

public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CreatedBy { get; set; }
    public User Creator { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Open { get; set; } = true;

    public ICollection<PositionAttribute> PositionAttributes { get; set; } = [];
    public ICollection<PositionAccessRule> AccessRules { get; set; } = [];
    public ICollection<Cv> Cvs { get; set; } = [];
    public Discussion? Discussion { get; set; }
}