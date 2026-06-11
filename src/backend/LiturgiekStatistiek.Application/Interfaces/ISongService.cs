using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface ISongService
{
    Task<PaginatedResult<SongDto>> GetSongsByBundleAsync(Guid bundleId, int page = 1, int pageSize = 50);
    Task<SongDto?> GetSongByIdAsync(Guid id);
    Task<SongDto> CreateSongAsync(CreateSongRequest request, string userId);
    Task<SongDto?> UpdateSongAsync(Guid id, UpdateSongRequest request, string userId);
    Task<bool> DeleteSongAsync(Guid id);
}
