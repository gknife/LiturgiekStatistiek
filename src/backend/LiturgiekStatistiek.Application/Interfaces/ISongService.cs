using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface ISongService
{
    Task<PaginatedResult<SongDto>> GetSongsByBundleAsync(Guid bundleId, int page = 1, int pageSize = 50);
    Task<SongDto?> GetSongByIdAsync(Guid id);
    Task<SongDto?> GetSongByNumberAsync(Guid bundleId, int number);
    Task<SongDto> CreateSongAsync(CreateSongRequest request, string userId);
    Task<SongDto?> UpdateSongAsync(Guid id, UpdateSongRequest request, string userId);
    Task<bool> DeleteSongAsync(Guid id);

    // --- Per-bundle rubrieken (categorieën) ---
    Task<IReadOnlyList<BundleSectionDto>> GetSectionsAsync(Guid bundleId);
    Task<BundleSectionDto> CreateSectionAsync(Guid bundleId, CreateBundleSectionRequest request, string userId);
    Task<BundleSectionDto?> UpdateSectionAsync(Guid id, UpdateBundleSectionRequest request, string userId);
    Task<bool> DeleteSectionAsync(Guid id);
}
