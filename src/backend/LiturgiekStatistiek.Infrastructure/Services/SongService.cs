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
                    .OrderBy(v => v.Number)
                    .Select(v => new SongVerseDto(v.Number, v.Title))
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
                    .OrderBy(v => v.Number)
                    .Select(v => new SongVerseDto(v.Number, v.Title))
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
            NumberOfVerses = request.NumberOfVerses ?? request.Verses?.Count,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Songs.Add(song);

        if (request.Verses is { Count: > 0 })
        {
            foreach (var v in request.Verses)
            {
                _context.SongCatalogVerses.Add(new SongCatalogVerse
                {
                    Id = Guid.NewGuid(),
                    SongId = song.Id,
                    Number = v.Number,
                    Title = v.Title
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
        song.NumberOfVerses = request.NumberOfVerses ?? request.Verses?.Count;
        song.ModifiedBy = userId;
        song.ModifiedAt = DateTime.UtcNow;

        if (request.Verses != null)
        {
            _context.SongCatalogVerses.RemoveRange(song.Verses);
            foreach (var v in request.Verses)
            {
                _context.SongCatalogVerses.Add(new SongCatalogVerse
                {
                    Id = Guid.NewGuid(),
                    SongId = song.Id,
                    Number = v.Number,
                    Title = v.Title
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
}
