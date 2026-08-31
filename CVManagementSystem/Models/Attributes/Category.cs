namespace CVManagementSystem.Models.Attributes;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<AttributeDefinition> Attributes { get; set; } = [];
}