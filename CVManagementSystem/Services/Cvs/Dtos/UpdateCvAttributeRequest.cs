namespace CVManagementSystem.Services.Cvs.Dtos;

public class UpdateCvAttributeRequest
{
    public int AttributeId { get; set; }
    public string? TextValue { get; set; }
    public double? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
}