namespace CVManagementSystem.Services.Dashboard.Dtos;

using CVManagementSystem.Services.Positions.Dtos;

public class TagCloudItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardStatsDto
{
    public int CvsCreatedToday { get; set; }
    public int TotalPositions { get; set; }
    public int TotalCandidates { get; set; }
    public int TotalRecruiters { get; set; }
    public int TotalCvs { get; set; }
}

public class DashboardDto
{
    public List<PositionListItemDto> RecentPositions { get; set; } = [];
    public List<PositionListItemDto> TopPositions { get; set; } = [];
    public List<TagCloudItemDto> TagCloud { get; set; } = [];
    public DashboardStatsDto Stats { get; set; } = new();
}