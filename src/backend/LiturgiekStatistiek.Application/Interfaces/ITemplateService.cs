using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface ITemplateService
{
    Task<List<ServiceTemplateSummaryDto>> GetTemplatesAsync();
    Task<ServiceTemplateDto?> GetTemplateByIdAsync(Guid id);
    Task<ServiceTemplateDto> CreateTemplateAsync(CreateServiceTemplateRequest request, string userId);
    Task<ServiceTemplateDto?> UpdateTemplateAsync(Guid id, CreateServiceTemplateRequest request, string userId);
    Task<bool> DeleteTemplateAsync(Guid id);

    /// <summary>
    /// Finds the best-matching active template for the given selectors and returns
    /// its onderdelen as ready-to-use service element requests (no week-specific
    /// content). Returns null when no template matches.
    /// </summary>
    Task<List<CreateServiceElementRequest>?> InstantiateAsync(Guid? denominationId, Guid? congregationId, int? timeOfDay, Guid? occasionId);

    /// <summary>Resolves the best-matching template (as a DTO) for the given selectors, or null.</summary>
    Task<ServiceTemplateDto?> ResolveAsync(Guid? denominationId, Guid? congregationId, int? timeOfDay, Guid? occasionId);
}
