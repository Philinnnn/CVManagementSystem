namespace CVManagementSystem.Services.Search.Dtos;

using Positions.Dtos;

public class CandidateSearchResultDto
{
    public int CandidateId { get; set; }
    public string Fullname { get; set; } = string.Empty;
}

public class SearchResultsDto
{
    public List<PositionListItemDto> Positions { get; set; } = [];
    public List<CandidateSearchResultDto> Candidates { get; set; } = [];
}