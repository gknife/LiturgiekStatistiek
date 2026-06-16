using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class SavedQueryService : ISavedQueryService
{
    private readonly IApplicationDbContext _db;

    public SavedQueryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<SavedQueryDto>> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        var items = await _db.SavedQueries
            .Where(q => q.UserId == userId || q.IsPublic)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<SavedQueryDto?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var item = await _db.SavedQueries
            .FirstOrDefaultAsync(q => q.Id == id && (q.UserId == userId || q.IsPublic), ct);
        return item == null ? null : Map(item);
    }

    public async Task<SavedQueryDto> CreateAsync(SaveQueryRequest request, string userId, CancellationToken ct = default)
    {
        var entity = new SavedQuery
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            QueryParameters = request.QueryParameters,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow
        };

        _db.SavedQueries.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var item = await _db.SavedQueries.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId, ct);
        if (item == null) return false;

        _db.SavedQueries.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static SavedQueryDto Map(SavedQuery q) => new()
    {
        Id = q.Id,
        Name = q.Name,
        QueryParameters = q.QueryParameters,
        IsPublic = q.IsPublic,
        CreatedAt = q.CreatedAt
    };
}
