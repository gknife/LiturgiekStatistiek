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
            .OrderBy(s => s.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SongDto(
                s.Id,
                s.BundleId,
                s.Bundle.Value,
                s.Bundle.Abbreviation,
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
            .Where(s => s.Id == id)
            .Select(s => new SongDto(
                s.Id,
                s.BundleId,
                s.Bundle.Value,
                s.Bundle.Abbreviation,
                s.Number,
                s.Title,
                s.NumberOfVerses))
            .FirstOrDefaultAsync();
    }

    public async Task<SongDto> CreateSongAsync(CreateSongRequest request, string userId)
    {
        var song = new Song
        {
            Id = Guid.NewGuid(),
            BundleId = request.BundleId,
            Number = request.Number,
            Title = request.Title,
            NumberOfVerses = request.NumberOfVerses,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Songs.Add(song);
        await _context.SaveChangesAsync();

        return await GetSongByIdAsync(song.Id) ?? throw new InvalidOperationException();
    }

    public async Task<SongDto?> UpdateSongAsync(Guid id, UpdateSongRequest request, string userId)
    {
        var song = await _context.Songs.FindAsync(id);
        if (song == null)
        {
            return null;
        }

        song.Title = request.Title;
        song.NumberOfVerses = request.NumberOfVerses;
        song.ModifiedBy = userId;
        song.ModifiedAt = DateTime.UtcNow;

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
