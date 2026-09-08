namespace CVManagementSystem.Services.Positions.Dtos;

public class AccessRuleInput
{
    public int AttributeId { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
}