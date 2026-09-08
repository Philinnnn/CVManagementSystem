namespace CVManagementSystem.Services.Candidates.Dtos;

public class AttributeValueDto
{
    public int AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
    public List<string> SelectOptions { get; set; } = [];

    public string? TextValue { get; set; }
    public double? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }

    public bool IsEmpty =>
        TextValue is null && NumericValue is null && DateValue is null &&
        BooleanValue is null && DateRangeStart is null && DateRangeEnd is null;
}