namespace CVManagementSystem.Models.Candidates;

public class ProjectTag
{
    public int CandidateProjectId { get; set; }
    public CandidateProject CandidateProject { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}