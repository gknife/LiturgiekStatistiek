using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly ApplicationDbContext _context;

    public TemplateService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceTemplateSummaryDto>> GetTemplatesAsync()
    {
        return await _context.ServiceTemplates
            .Include(t => t.Denomination)
            .Include(t => t.Congregation)
            .Include(t => t.Occasion)
            .OrderBy(t => t.Name)
            .Select(t => new ServiceTemplateSummaryDto(
                t.Id,
                t.Name,
                t.DenominationId,
                t.Denomination != null ? t.Denomination.Value : null,
                t.CongregationId,
                t.Congregation != null ? t.Congregation.Name : null,
                t.TimeOfDay.HasValue ? (int)t.TimeOfDay.Value : (int?)null,
                t.OccasionId,
                t.Occasion != null ? t.Occasion.Value : null,
                t.IsActive,
                t.Elements.Count))
            .ToListAsync();
    }

    public async Task<ServiceTemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        var template = await _context.ServiceTemplates
            .Include(t => t.Denomination)
            .Include(t => t.Congregation)
            .Include(t => t.Occasion)
            .Include(t => t.MusicalAccompaniment)
            .Include(t => t.DefaultBibleTranslation)
            .Include(t => t.DefaultSongBundle)
            .Include(t => t.Elements).ThenInclude(e => e.Label)
            .Include(t => t.Elements).ThenInclude(e => e.Performer)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return template == null ? null : MapToDto(template);
    }

    public async Task<ServiceTemplateDto> CreateTemplateAsync(CreateServiceTemplateRequest request, string userId)
    {
        var template = new ServiceTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DenominationId = request.DenominationId,
            CongregationId = request.CongregationId,
            TimeOfDay = request.TimeOfDay.HasValue ? (TimeOfDay)request.TimeOfDay.Value : null,
            OccasionId = request.OccasionId,
            IsActive = request.IsActive,
            MusicalAccompanimentId = request.MusicalAccompanimentId,
            IsReadingService = request.IsReadingService,
            HasBeamerLiturgy = request.HasBeamerLiturgy,
            HasBeamerTexts = request.HasBeamerTexts,
            HasBeamerSongs = request.HasBeamerSongs,
            DefaultBibleTranslationId = request.DefaultBibleTranslationId,
            DefaultSongBundleId = request.DefaultSongBundleId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Elements = BuildElements(request.Elements)
        };

        _context.ServiceTemplates.Add(template);
        await _context.SaveChangesAsync();

        return (await GetTemplateByIdAsync(template.Id))!;
    }

    public async Task<ServiceTemplateDto?> UpdateTemplateAsync(Guid id, CreateServiceTemplateRequest request, string userId)
    {
        var template = await _context.ServiceTemplates
            .Include(t => t.Elements)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return null;
        }

        template.Name = request.Name;
        template.DenominationId = request.DenominationId;
        template.CongregationId = request.CongregationId;
        template.TimeOfDay = request.TimeOfDay.HasValue ? (TimeOfDay)request.TimeOfDay.Value : null;
        template.OccasionId = request.OccasionId;
        template.IsActive = request.IsActive;
        template.MusicalAccompanimentId = request.MusicalAccompanimentId;
        template.IsReadingService = request.IsReadingService;
        template.HasBeamerLiturgy = request.HasBeamerLiturgy;
        template.HasBeamerTexts = request.HasBeamerTexts;
        template.HasBeamerSongs = request.HasBeamerSongs;
        template.DefaultBibleTranslationId = request.DefaultBibleTranslationId;
        template.DefaultSongBundleId = request.DefaultSongBundleId;
        template.ModifiedBy = userId;
        template.ModifiedAt = DateTime.UtcNow;

        _context.ServiceTemplateElements.RemoveRange(template.Elements);
        template.Elements.Clear();
        await _context.SaveChangesAsync();

        foreach (var element in BuildElements(request.Elements))
        {
            element.ServiceTemplateId = template.Id;
            _context.ServiceTemplateElements.Add(element);
        }
        await _context.SaveChangesAsync();

        return await GetTemplateByIdAsync(template.Id);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var template = await _context.ServiceTemplates.FindAsync(id);
        if (template == null)
        {
            return false;
        }

        _context.ServiceTemplates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CreateServiceElementRequest>?> InstantiateAsync(Guid? denominationId, Guid? congregationId, int? timeOfDay, Guid? occasionId)
    {
        var template = await ResolveAsync(denominationId, congregationId, timeOfDay, occasionId);
        if (template == null)
        {
            return null;
        }

        return template.Elements
            .OrderBy(e => e.Position)
            .Select(e => new CreateServiceElementRequest(
                Position: e.Position,
                ElementType: e.ElementTypeValue,
                LabelId: e.LabelId,
                ScriptureReference: e.FixedScriptureReference,
                Notes: null,
                Songs: null,
                PerformerId: e.PerformerId,
                IsBeurtzang: e.IsBeurtzang,
                BibleTranslationId: null,
                ReadingReferences: null))
            .ToList();
    }

    public async Task<ServiceTemplateDto?> ResolveAsync(Guid? denominationId, Guid? congregationId, int? timeOfDay, Guid? occasionId)
    {
        var candidates = await _context.ServiceTemplates
            .Include(t => t.Denomination)
            .Include(t => t.Congregation)
            .Include(t => t.Occasion)
            .Include(t => t.MusicalAccompaniment)
            .Include(t => t.DefaultBibleTranslation)
            .Include(t => t.DefaultSongBundle)
            .Include(t => t.Elements).ThenInclude(e => e.Label)
            .Include(t => t.Elements).ThenInclude(e => e.Performer)
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync();

        ServiceTemplate? best = null;
        var bestScore = int.MinValue;

        foreach (var t in candidates)
        {
            // Hard filters: a template's non-null selectors must match the request.
            if (t.CongregationId.HasValue && t.CongregationId != congregationId) continue;
            if (t.DenominationId.HasValue && denominationId.HasValue && t.DenominationId != denominationId) continue;
            if (t.DenominationId.HasValue && !denominationId.HasValue) continue;
            if (t.TimeOfDay.HasValue && timeOfDay.HasValue && (int)t.TimeOfDay.Value != timeOfDay) continue;
            if (t.OccasionId.HasValue && occasionId.HasValue && t.OccasionId != occasionId) continue;
            if (t.OccasionId.HasValue && !occasionId.HasValue) continue;

            // Specificity score: more specific matches win.
            var score = 0;
            if (t.CongregationId.HasValue) score += 8;
            if (t.OccasionId.HasValue) score += 4;
            if (t.TimeOfDay.HasValue) score += 2;
            if (t.DenominationId.HasValue) score += 1;

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best == null ? null : MapToDto(best);
    }

    private static List<ServiceTemplateElement> BuildElements(List<CreateServiceTemplateElementRequest> requests)
    {
        return requests
            .OrderBy(e => e.Position)
            .Select(e => new ServiceTemplateElement
            {
                Id = Guid.NewGuid(),
                Position = e.Position,
                ElementType = (ElementType)e.ElementType,
                LabelId = e.LabelId,
                PerformerId = e.PerformerId,
                IsBeurtzang = e.IsBeurtzang,
                FixedScriptureReference = e.FixedScriptureReference
            })
            .ToList();
    }

    private static ServiceTemplateDto MapToDto(ServiceTemplate t)
    {
        return new ServiceTemplateDto(
            t.Id,
            t.Name,
            t.DenominationId,
            t.Denomination?.Value,
            t.CongregationId,
            t.Congregation?.Name,
            t.TimeOfDay.HasValue ? (int)t.TimeOfDay.Value : null,
            t.OccasionId,
            t.Occasion?.Value,
            t.IsActive,
            t.Elements
                .OrderBy(e => e.Position)
                .Select(e => new ServiceTemplateElementDto(
                    e.Id,
                    e.Position,
                    e.ElementType.ToString(),
                    (int)e.ElementType,
                    e.LabelId,
                    e.Label?.Value,
                    e.PerformerId,
                    e.Performer?.Value,
                    e.IsBeurtzang,
                    e.FixedScriptureReference))
                .ToList(),
            t.MusicalAccompanimentId,
            t.MusicalAccompaniment?.Value,
            t.IsReadingService,
            t.HasBeamerLiturgy,
            t.HasBeamerTexts,
            t.HasBeamerSongs,
            t.DefaultBibleTranslationId,
            t.DefaultBibleTranslation?.Value,
            t.DefaultSongBundleId,
            t.DefaultSongBundle?.Value);
    }
}
