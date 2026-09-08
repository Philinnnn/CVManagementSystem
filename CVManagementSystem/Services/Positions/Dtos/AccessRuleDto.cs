namespace CVManagementSystem.Services.Positions.Dtos;

public class AccessRuleDto
{
    public int Id { get; set; }
    public int AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
}