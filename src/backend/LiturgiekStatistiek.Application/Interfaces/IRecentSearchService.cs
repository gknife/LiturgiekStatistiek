using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IRecentSearchService
{
    /// <summary>Returns the user's most recent searches, newest first.</summary>
    Task<List<RecentSearchDto>> GetForUserAsync(string userId, int max = 10, CancellationToken ct = default);

    /// <summary>Records a search; de-duplicates identical recent text and trims history.</summary>
    Task<RecentSearchDto> AddAsync(string userId, AddRecentSearchRequest request, CancellationToken ct = default);

    /// <summary>Removes all recent searches for the user.</summary>
    Task ClearAsync(string userId, CancellationToken ct = default);
}
