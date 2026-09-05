namespace CVManagementSystem.Models.Candidates;

using Cvs;
using Identity;

public class Candidate
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Version { get; set; } = 1;

    public ICollection<CandidateAttributeValue> AttributeValues { get; set; } = [];
    public ICollection<CandidateProject> Projects { get; set; } = [];
    public ICollection<Cv> Cvs { get; set; } = [];
}