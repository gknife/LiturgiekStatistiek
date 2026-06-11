using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IContentService
{
    Task<ContentPageDto?> GetContentBySlugAsync(string slug);
    Task<ContentPageDto?> UpdateContentAsync(string slug, UpdateContentPageRequest request, string userId);
}
