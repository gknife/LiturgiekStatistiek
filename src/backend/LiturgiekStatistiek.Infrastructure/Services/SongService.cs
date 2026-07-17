using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class SongService : ISongService
{
    private readonly ApplicationDbContext _context;

    public SongService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<SongDto>> GetSongsByBundleAsync(Guid bundleId, int page = 1, int pageSize = 50)
    {
        var query = _context.Songs
            .Include(s => s.Bundle)
            .Where(s => s.BundleId == bundleId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Section)
            .ThenBy(s => s.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SongDto(
                s.Id,
                s.BundleId,
                s.Bundle.Value,
                s.Bundle.Abbreviation,
                s.Section,
                s.Number,
                s.Title,
                s.NumberOfVerses))
            .ToListAsync();

        return new PaginatedResult<SongDto>(items, totalCount, page, pageSize);
    }

    public async Task<SongDto?> GetSongByIdAsync(Guid id)
    {
        return await _context.Songs
            .Include(s => s.Bundle)
            .Include(s => s.Verses)
            .Where(s => s.Id == id)
            .Select(s => new SongDto(
                s.Id,
                s.BundleId,
                s.Bundle.Value,
                s.Bundle.Abbreviation,
                s.Section,
                s.Number,
                s.Title,
                s.NumberOfVerses,
                s.Verses
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.Number)
                    .Select(v => new SongVerseDto(v.Number, v.Title, v.Label, v.SortOrder))
                    .ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<SongDto?> GetSongByNumberAsync(Guid bundleId, int number)
    {
        return await _context.Songs
            .Include(s => s.Bundle)
            .Include(s => s.Verses)
            .Where(s => s.BundleId == bundleId && s.Number == number)
            .OrderBy(s => s.Section)
            .Select(s => new SongDto(
                s.Id,
                s.BundleId,
                s.Bundle.Value,
                s.Bundle.Abbreviation,
                s.Section,
                s.Number,
                s.Title,
                s.NumberOfVerses,
                s.Verses
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.Number)
                    .Select(v => new SongVerseDto(v.Number, v.Title, v.Label, v.SortOrder))
                    .ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<SongDto> CreateSongAsync(CreateSongRequest request, string userId)
    {
        var song = new Song
        {
            Id = Guid.NewGuid(),
            BundleId = request.BundleId,
            Section = request.Section ?? "",
            Number = request.Number,
            Title = request.Title,
            NumberOfVerses = request.NumberOfVerses ?? request.Verses?.Count(v => string.IsNullOrWhiteSpace(v.Label)),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Songs.Add(song);

        if (request.Verses is { Count: > 0 })
        {
            var order = 0;
            foreach (var v in request.Verses)
            {
                _context.SongCatalogVerses.Add(new SongCatalogVerse
                {
                    Id = Guid.NewGuid(),
                    SongId = song.Id,
                    Number = v.Number,
                    Title = v.Title,
                    Label = string.IsNullOrWhiteSpace(v.Label) ? null : v.Label.Trim(),
                    SortOrder = order++
                });
            }
        }

        await _context.SaveChangesAsync();

        return await GetSongByIdAsync(song.Id) ?? throw new InvalidOperationException();
    }

    public async Task<SongDto?> UpdateSongAsync(Guid id, UpdateSongRequest request, string userId)
    {
        var song = await _context.Songs
            .Include(s => s.Verses)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (song == null)
        {
            return null;
        }

        if (request.Section != null) song.Section = request.Section;
        if (request.Number.HasValue) song.Number = request.Number.Value;
        song.Title = request.Title;
        song.NumberOfVerses = request.NumberOfVerses ?? request.Verses?.Count(v => string.IsNullOrWhiteSpace(v.Label));
        song.ModifiedBy = userId;
        song.ModifiedAt = DateTime.UtcNow;

        if (request.Verses != null)
        {
            _context.SongCatalogVerses.RemoveRange(song.Verses);
            var order = 0;
            foreach (var v in request.Verses)
            {
                _context.SongCatalogVerses.Add(new SongCatalogVerse
                {
                    Id = Guid.NewGuid(),
                    SongId = song.Id,
                    Number = v.Number,
                    Title = v.Title,
                    Label = string.IsNullOrWhiteSpace(v.Label) ? null : v.Label.Trim(),
                    SortOrder = order++
                });
            }
        }

        await _context.SaveChangesAsync();
        return await GetSongByIdAsync(id);
    }

    public async Task<bool> DeleteSongAsync(Guid id)
    {
        var song = await _context.Songs.FindAsync(id);
        if (song == null)
        {
            return false;
        }

        _context.Songs.Remove(song);
        await _context.SaveChangesAsync();
        return true;
    }

    // --- Per-bundle rubrieken (categorieën) ---

    public async Task<IReadOnlyList<BundleSectionDto>> GetSectionsAsync(Guid bundleId)
    {
        return await _context.BundleSections
            .Where(s => s.BundleId == bundleId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Value)
            .Select(s => new BundleSectionDto(s.Id, s.BundleId, s.Value, s.SortOrder, s.IsDefault, s.IsActive))
            .ToListAsync();
    }

    public async Task<BundleSectionDto> CreateSectionAsync(Guid bundleId, CreateBundleSectionRequest request, string userId)
    {
        var section = new BundleSection
        {
            Id = Guid.NewGuid(),
            BundleId = bundleId,
            Value = request.Value.Trim(),
            SortOrder = request.SortOrder,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.BundleSections.Add(section);

        if (section.IsDefault)
        {
            await ClearOtherDefaultsAsync(bundleId, section.Id);
        }

        await _context.SaveChangesAsync();
        return new BundleSectionDto(section.Id, section.BundleId, section.Value, section.SortOrder, section.IsDefault, section.IsActive);
    }

    public async Task<BundleSectionDto?> UpdateSectionAsync(Guid id, UpdateBundleSectionRequest request, string userId)
    {
        var section = await _context.BundleSections.FirstOrDefaultAsync(s => s.Id == id);
        if (section == null)
        {
            return null;
        }

        var oldValue = section.Value;
        var newValue = request.Value.Trim();

        section.Value = newValue;
        section.SortOrder = request.SortOrder;
        section.IsDefault = request.IsDefault;
        section.IsActive = request.IsActive;
        section.ModifiedBy = userId;
        section.ModifiedAt = DateTime.UtcNow;

        // Cascade a rename to existing songs and service song references in this bundle
        // so the rubriek text stays consistent everywhere it is stored.
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            var songs = await _context.Songs
                .Where(s => s.BundleId == section.BundleId && s.Section == oldValue)
                .ToListAsync();
            foreach (var s in songs) s.Section = newValue;

            var refs = await _context.ServiceElementSongs
                .Where(s => s.BundleId == section.BundleId && s.Section == oldValue)
                .ToListAsync();
            foreach (var r in refs) r.Section = newValue;
        }

        if (section.IsDefault)
        {
            await ClearOtherDefaultsAsync(section.BundleId, section.Id);
        }

        await _context.SaveChangesAsync();
        return new BundleSectionDto(section.Id, section.BundleId, section.Value, section.SortOrder, section.IsDefault, section.IsActive);
    }

    public async Task<bool> DeleteSectionAsync(Guid id)
    {
        var section = await _context.BundleSections.FindAsync(id);
        if (section == null)
        {
            return false;
        }

        _context.BundleSections.Remove(section);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ClearOtherDefaultsAsync(Guid bundleId, Guid keepId)
    {
        var others = await _context.BundleSections
            .Where(s => s.BundleId == bundleId && s.Id != keepId && s.IsDefault)
            .ToListAsync();
        foreach (var o in others) o.IsDefault = false;
    }
}
