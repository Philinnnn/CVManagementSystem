namespace CVManagementSystem.Models.Positions;

using Attributes;

public class PositionAttribute
{
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;

    public bool Required { get; set; }
}