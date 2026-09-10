namespace CVManagementSystem.Models.Cvs;

using Candidates;
using Positions;

public class Cv
{
    public int Id { get; set; }
    public CvStatus Status { get; set; } = CvStatus.Draft;
    public int CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public int Version { get; set; } = 1;

    public ICollection<CvAttributeValue> AttributeValues { get; set; } = [];
    public ICollection<CvLike> Likes { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}