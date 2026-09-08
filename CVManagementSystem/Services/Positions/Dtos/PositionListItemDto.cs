namespace CVManagementSystem.Services.Positions.Dtos;

public class PositionListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Open { get; set; }
    public int CvCount { get; set; }
}