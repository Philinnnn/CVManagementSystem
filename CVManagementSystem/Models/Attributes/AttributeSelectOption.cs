namespace CVManagementSystem.Models.Attributes;

public class AttributeSelectOption
{
    public long Id { get; set; }
    
    public int AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;

    public string OptionValue { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}