using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IPreacherService
{
    Task<PaginatedResult<PreacherDto>> GetPreachersAsync(int page = 1, int pageSize = 50, string? search = null);
    Task<PreacherDto?> GetPreacherByIdAsync(Guid id);
    Task<PreacherDto> CreatePreacherAsync(CreatePreacherRequest request, string userId);
    Task<PreacherDto?> UpdatePreacherAsync(Guid id, UpdatePreacherRequest request, string userId);
    Task<List<PreacherSummaryDto>> SearchPreachersAsync(string query);
}
