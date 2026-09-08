namespace CVManagementSystem.Services.Positions.Dtos;

public class UpdatePositionRequest : CreatePositionRequest
{
    public int Id { get; set; }
    public int Version { get; set; }
    public bool Open { get; set; }
}