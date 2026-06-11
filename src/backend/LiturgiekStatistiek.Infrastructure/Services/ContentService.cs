using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class ContentService : IContentService
{
    private readonly ApplicationDbContext _context;

    public ContentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContentPageDto?> GetContentBySlugAsync(string slug)
    {
        return await _context.ContentPages
            .Where(p => p.Slug == slug)
            .Select(p => new ContentPageDto(
                p.Id,
                p.Slug,
                p.TitleNl,
                p.ContentMarkdown,
                p.ModifiedBy,
                p.ModifiedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<ContentPageDto?> UpdateContentAsync(string slug, UpdateContentPageRequest request, string userId)
    {
        var page = await _context.ContentPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page == null)
        {
            return null;
        }

        page.TitleNl = request.TitleNl;
        page.ContentMarkdown = request.ContentMarkdown;
        page.ModifiedBy = userId;
        page.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return new ContentPageDto(page.Id, page.Slug, page.TitleNl, page.ContentMarkdown, page.ModifiedBy, page.ModifiedAt);
    }
}
