using LiturgiekStatistiek.Application.DTOs.Queries;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface ISavedQueryService
{
    /// <summary>Returns the user's own saved queries plus any public ones.</summary>
    Task<List<SavedQueryDto>> GetForUserAsync(string userId, CancellationToken ct = default);

    Task<SavedQueryDto?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);

    Task<SavedQueryDto> CreateAsync(SaveQueryRequest request, string userId, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default);
}
