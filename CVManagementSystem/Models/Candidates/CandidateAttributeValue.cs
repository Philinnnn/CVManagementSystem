namespace CVManagementSystem.Models.Candidates;

using Attributes;

public class CandidateAttributeValue
{
    public int CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    
    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;

    public string? TextValue { get; set; }
    public double? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
}