namespace CVManagementSystem.Models.Cvs;

using Attributes;

public class CvAttributeValue
{
    public long Id { get; set; }

    public int CvId { get; set; }
    public Cv Cv { get; set; } = null!;

    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;

    public string? TextValue { get; set; }
    public double? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
}