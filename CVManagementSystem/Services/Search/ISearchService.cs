namespace CVManagementSystem.Services.Search;

using Dtos;

public interface ISearchService
{
    Task<SearchResultsDto> SearchAsync(string? query);
}