namespace CVManagementSystem.Services.Attributes.Dtos;

public class AttributeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<string> SelectOptions { get; set; } = [];
    public bool IsBuiltIn { get; set; }
}