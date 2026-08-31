namespace CVManagementSystem.Models.Positions;

using Attributes;

public class PositionAccessRule
{
    public int Id { get; set; }

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;

    public string Operator { get; set; } = string.Empty; // ">", "<", ">=", "<=", "==", "!="
    public string ExpectedValue { get; set; } = string.Empty;
}