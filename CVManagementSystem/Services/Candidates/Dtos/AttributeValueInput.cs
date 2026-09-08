namespace CVManagementSystem.Services.Candidates.Dtos;

public class AttributeValueInput
{
    public int AttributeId { get; set; }
    public string? TextValue { get; set; }
    public double? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
}