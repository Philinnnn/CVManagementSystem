namespace CVManagementSystem.Services.Positions.Dtos;

public class AttributeAggregateDto
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int FilledCount { get; set; }
    public int TotalCount { get; set; }
    public double? Average { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public List<ValueCountDto> TopValues { get; set; } = [];
}

public class ValueCountDto
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PositionAggregateDto
{
    public string PositionTitle { get; set; } = string.Empty;
    public List<AttributeAggregateDto> Attributes { get; set; } = [];
}