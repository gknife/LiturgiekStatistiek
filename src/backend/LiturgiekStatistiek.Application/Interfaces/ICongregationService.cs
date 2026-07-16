using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface ICongregationService
{
    Task<PaginatedResult<CongregationDto>> GetCongregationsAsync(int page = 1, int pageSize = 50, string? search = null);
    Task<CongregationDto?> GetCongregationByIdAsync(Guid id);
    Task<CongregationDto> CreateCongregationAsync(CreateCongregationRequest request, string userId);
    Task<CongregationDto?> UpdateCongregationAsync(Guid id, UpdateCongregationRequest request, string userId);
    Task<DeleteOutcome> DeleteCongregationAsync(Guid id, string userId);
    Task<List<CongregationSummaryDto>> SearchCongregationsAsync(string query);
}
