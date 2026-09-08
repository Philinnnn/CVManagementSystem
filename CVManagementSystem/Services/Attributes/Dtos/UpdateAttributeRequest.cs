namespace CVManagementSystem.Services.Attributes.Dtos;

public class UpdateAttributeRequest
{
    public int Id { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<string> SelectOptions { get; set; } = [];
}