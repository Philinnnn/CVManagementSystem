namespace CVManagementSystem.Services.Positions.Dtos;

public class PositionAttributeDto
{
    public int AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool Required { get; set; }
}