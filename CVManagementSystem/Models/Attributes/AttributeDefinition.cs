using CVManagementSystem.Models.Positions;

namespace CVManagementSystem.Models.Attributes;

public class AttributeDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string DataType { get; set; } = string.Empty; // String, Numeric, Date, Boolean, Period, Select

    public ICollection<AttributeSelectOption> SelectOptions { get; set; } = [];
    public ICollection<PositionAttribute> PositionAttributes { get; set; } = [];
    public ICollection<PositionAccessRule> PositionAccessRules { get; set; } = [];
}