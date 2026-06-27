using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class RecentSearchService : IRecentSearchService
{
    private const int MaxStored = 50;
    private readonly IApplicationDbContext _db;

    public RecentSearchService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<RecentSearchDto>> GetForUserAsync(string userId, int max = 10, CancellationToken ct = default)
    {
        var items = await _db.RecentSearches
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(max)
            .ToListAsync(ct);

        return items.Select(s => new RecentSearchDto(s.Id, s.QueryText, s.CreatedAt)).ToList();
    }

    public async Task<RecentSearchDto> AddAsync(string userId, AddRecentSearchRequest request, CancellationToken ct = default)
    {
        var text = (request.QueryText ?? string.Empty).Trim();

        // De-duplicate: drop any previous identical search so it bubbles to the top.
        var duplicates = await _db.RecentSearches
            .Where(s => s.UserId == userId && s.QueryText == text)
            .ToListAsync(ct);
        if (duplicates.Count > 0)
            _db.RecentSearches.RemoveRange(duplicates);

        var entity = new RecentSearch
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = text,
            CreatedAt = DateTime.UtcNow,
        };
        _db.RecentSearches.Add(entity);

        // Trim history to keep storage bounded.
        var overflow = await _db.RecentSearches
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(MaxStored)
            .ToListAsync(ct);
        if (overflow.Count > 0)
            _db.RecentSearches.RemoveRange(overflow);

        await _db.SaveChangesAsync(ct);
        return new RecentSearchDto(entity.Id, entity.QueryText, entity.CreatedAt);
    }

    public async Task ClearAsync(string userId, CancellationToken ct = default)
    {
        var items = await _db.RecentSearches.Where(s => s.UserId == userId).ToListAsync(ct);
        if (items.Count > 0)
        {
            _db.RecentSearches.RemoveRange(items);
            await _db.SaveChangesAsync(ct);
        }
    }
}
