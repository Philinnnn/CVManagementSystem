namespace CVManagementSystem.Services.Candidates.Dtos;

public class SaveProfileRequest
{
    public int Version { get; set; }
    public List<AttributeValueInput> Values { get; set; } = [];
}