namespace CVManagementSystem.Services.Dashboard;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Services.Positions.Dtos;

public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var recentPositions = await db.Positions
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .Select(ToListItem)
            .ToListAsync();

        var topPositions = await db.Positions
            .OrderByDescending(p => p.Cvs.Count)
            .Take(5)
            .Select(ToListItem)
            .ToListAsync();

        var projectTagCounts = await db.ProjectTags
            .GroupBy(pt => pt.Tag.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        var positionTagCounts = await db.PositionTags
            .GroupBy(pt => pt.Tag.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();

        var tagCloud = projectTagCounts.Concat(positionTagCounts)
            .GroupBy(t => t.Name)
            .Select(g => new TagCloudItemDto { Name = g.Key, Count = g.Sum(x => x.Count) })
            .OrderByDescending(t => t.Count)
            .Take(30)
            .ToList();

        var today = DateTime.UtcNow.Date;

        var stats = new DashboardStatsDto
        {
            CvsCreatedToday = await db.Cvs.CountAsync(c => c.CreatedAt >= today),
            TotalPositions = await db.Positions.CountAsync(),
            TotalCandidates = await db.UserRoles.CountAsync(ur => ur.Role.Name == "Candidate"),
            TotalRecruiters = await db.UserRoles.CountAsync(ur => ur.Role.Name == "Recruiter"),
            TotalCvs = await db.Cvs.CountAsync()
        };

        return new DashboardDto
        {
            RecentPositions = recentPositions,
            TopPositions = topPositions,
            TagCloud = tagCloud,
            Stats = stats
        };
    }

    private static readonly System.Linq.Expressions.Expression<Func<Models.Positions.Position, PositionListItemDto>> ToListItem = p =>
        new PositionListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            CreatedAt = p.CreatedAt,
            Open = p.Open,
            CvCount = p.Cvs.Count
        };
}