namespace CVManagementSystem.Services.Search;

using Data;
using Dtos;
using Microsoft.EntityFrameworkCore;
using Services.Positions.Dtos;

public class SearchService(AppDbContext db) : ISearchService
{
    public async Task<SearchResultsDto> SearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResultsDto();

        var positions = await db.Positions
            .Where(p => p.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", query)))
            .Select(p => new PositionListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                CreatedAt = p.CreatedAt,
                Open = p.Open,
                CvCount = p.Cvs.Count
            })
            .Take(20)
            .ToListAsync();

        var candidates = await db.Users
            .Where(u => u.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", query)) && u.Candidate != null)
            .Select(u => new CandidateSearchResultDto
            {
                CandidateId = u.Candidate!.Id,
                Fullname = u.Fullname
            })
            .Take(20)
            .ToListAsync();

        return new SearchResultsDto { Positions = positions, Candidates = candidates };
    }
}