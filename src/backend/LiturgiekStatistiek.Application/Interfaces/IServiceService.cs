using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IServiceService
{
    Task<PaginatedResult<ServiceSummaryDto>> GetServicesAsync(int page = 1, int pageSize = 20, Guid? congregationId = null, DateOnly? fromDate = null, DateOnly? toDate = null);
    Task<ServiceDto?> GetServiceByIdAsync(Guid id);
    Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, string userId);
    Task<ServiceDto?> UpdateServiceAsync(Guid id, UpdateServiceRequest request, string userId);
    Task<bool> DeleteServiceAsync(Guid id);
}
