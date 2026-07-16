using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class PreacherService : IPreacherService
{
    private readonly ApplicationDbContext _context;

    public PreacherService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<PreacherDto>> GetPreachersAsync(int page = 1, int pageSize = 50, string? search = null)
    {
        var query = _context.Preachers
            .Include(p => p.Denomination)
            .Include(p => p.Title)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.FullName.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PreacherDto(
                p.Id,
                p.FullName,
                p.Denomination != null ? p.Denomination.Value : null,
                p.City,
                p.DenominationId,
                p.TitleId,
                p.Title != null ? p.Title.Value : null,
                p.Services.Count))
            .ToListAsync();

        return new PaginatedResult<PreacherDto>(items, totalCount, page, pageSize);
    }

    public async Task<PreacherDto?> GetPreacherByIdAsync(Guid id)
    {
        return await _context.Preachers
            .Include(p => p.Denomination)
            .Include(p => p.Title)
            .Where(p => p.Id == id)
            .Select(p => new PreacherDto(
                p.Id,
                p.FullName,
                p.Denomination != null ? p.Denomination.Value : null,
                p.City,
                p.DenominationId,
                p.TitleId,
                p.Title != null ? p.Title.Value : null,
                p.Services.Count))
            .FirstOrDefaultAsync();
    }

    public async Task<PreacherDto> CreatePreacherAsync(CreatePreacherRequest request, string userId)
    {
        var preacher = new Preacher
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            DenominationId = request.DenominationId,
            TitleId = request.TitleId,
            City = request.City,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Preachers.Add(preacher);
        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(Preacher),
            EntityId = preacher.Id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Created,
            PreviousValues = null
        });
        await _context.SaveChangesAsync();

        return await GetPreacherByIdAsync(preacher.Id) ?? throw new InvalidOperationException();
    }

    public async Task<PreacherDto?> UpdatePreacherAsync(Guid id, UpdatePreacherRequest request, string userId)
    {
        var preacher = await _context.Preachers.FindAsync(id);
        if (preacher == null)
        {
            return null;
        }

        var previous = System.Text.Json.JsonSerializer.Serialize(new
        {
            preacher.FullName,
            preacher.DenominationId,
            preacher.TitleId,
            preacher.City
        });

        preacher.FullName = request.FullName;
        preacher.DenominationId = request.DenominationId;
        preacher.TitleId = request.TitleId;
        preacher.City = request.City;
        preacher.ModifiedBy = userId;
        preacher.ModifiedAt = DateTime.UtcNow;

        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(Preacher),
            EntityId = preacher.Id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Updated,
            PreviousValues = previous
        });

        await _context.SaveChangesAsync();
        return await GetPreacherByIdAsync(id);
    }

    public async Task<DeleteOutcome> DeletePreacherAsync(Guid id, string userId)
    {
        var preacher = await _context.Preachers.FindAsync(id);
        if (preacher == null)
        {
            return DeleteOutcome.NotFound;
        }

        if (await _context.Services.AnyAsync(s => s.PreacherId == id))
        {
            return DeleteOutcome.HasReferences;
        }

        var previous = System.Text.Json.JsonSerializer.Serialize(new
        {
            preacher.FullName,
            preacher.DenominationId,
            preacher.TitleId,
            preacher.City
        });

        _context.Preachers.Remove(preacher);
        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(Preacher),
            EntityId = id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Deleted,
            PreviousValues = previous
        });
        await _context.SaveChangesAsync();
        return DeleteOutcome.Deleted;
    }

    public async Task<List<PreacherSummaryDto>> SearchPreachersAsync(string query)
    {
        return await _context.Preachers
            .Include(p => p.Title)
            .Include(p => p.Denomination)
            .Where(p => p.FullName.Contains(query))
            .OrderBy(p => p.FullName)
            .Take(10)
            .Select(p => new PreacherSummaryDto(
                p.Id,
                p.FullName,
                p.City,
                p.Title != null ? p.Title.Value : null,
                p.Denomination != null ? p.Denomination.Value : null))
            .ToListAsync();
    }
}
