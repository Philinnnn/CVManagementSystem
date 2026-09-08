namespace CVManagementSystem.Services.Attributes.Dtos;

public class CreateAttributeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string DataType { get; set; } = string.Empty;
    public List<string> SelectOptions { get; set; } = [];
}