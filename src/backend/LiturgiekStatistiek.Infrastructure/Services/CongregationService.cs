using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class CongregationService : ICongregationService
{
    private readonly ApplicationDbContext _context;

    public CongregationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<CongregationDto>> GetCongregationsAsync(int page = 1, int pageSize = 50, string? search = null)
    {
        var query = _context.Congregations
            .Include(c => c.Denomination)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search) || c.City.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.City)
            .ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CongregationDto(
                c.Id,
                c.Name,
                c.City,
                c.LocationDetail,
                c.Denomination != null ? c.Denomination.Value : null,
                c.Denomination != null ? c.Denomination.Abbreviation : null,
                c.Modality,
                c.Latitude,
                c.Longitude,
                c.DenominationId,
                c.Services.Count,
                c.Pastors
                    .Select(cp => new CongregationPastorDto(cp.PreacherId, cp.Preacher.FullName, cp.Preacher.City, cp.IsPrimary))
                    .ToList()))
            .ToListAsync();

        return new PaginatedResult<CongregationDto>(items, totalCount, page, pageSize);
    }

    public async Task<CongregationDto?> GetCongregationByIdAsync(Guid id)
    {
        return await _context.Congregations
            .Include(c => c.Denomination)
            .Where(c => c.Id == id)
            .Select(c => new CongregationDto(
                c.Id,
                c.Name,
                c.City,
                c.LocationDetail,
                c.Denomination != null ? c.Denomination.Value : null,
                c.Denomination != null ? c.Denomination.Abbreviation : null,
                c.Modality,
                c.Latitude,
                c.Longitude,
                c.DenominationId,
                c.Services.Count,
                c.Pastors
                    .Select(cp => new CongregationPastorDto(cp.PreacherId, cp.Preacher.FullName, cp.Preacher.City, cp.IsPrimary))
                    .ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<CongregationDto> CreateCongregationAsync(CreateCongregationRequest request, string userId)
    {
        var congregation = new Congregation
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            City = request.City,
            LocationDetail = request.LocationDetail,
            DenominationId = request.DenominationId,
            Modality = request.Modality,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Congregations.Add(congregation);
        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(Congregation),
            EntityId = congregation.Id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Created,
            PreviousValues = null
        });
        await _context.SaveChangesAsync();

        return await GetCongregationByIdAsync(congregation.Id) ?? throw new InvalidOperationException();
    }

    public async Task<CongregationDto?> UpdateCongregationAsync(Guid id, UpdateCongregationRequest request, string userId)
    {
        var congregation = await _context.Congregations
            .Include(c => c.Pastors)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (congregation == null)
        {
            return null;
        }

        var previous = System.Text.Json.JsonSerializer.Serialize(new
        {
            congregation.Name,
            congregation.City,
            congregation.LocationDetail,
            congregation.DenominationId,
            congregation.Modality,
            Pastors = congregation.Pastors.Select(p => new { p.PreacherId, p.IsPrimary })
        });

        congregation.Name = request.Name;
        congregation.City = request.City;
        congregation.LocationDetail = request.LocationDetail;
        congregation.DenominationId = request.DenominationId;
        congregation.Modality = request.Modality;
        congregation.Latitude = request.Latitude;
        congregation.Longitude = request.Longitude;
        congregation.ModifiedBy = userId;
        congregation.ModifiedAt = DateTime.UtcNow;

        if (request.Pastors != null)
        {
            SyncPastors(congregation, request.Pastors);
        }

        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(Congregation),
            EntityId = congregation.Id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Updated,
            PreviousValues = previous
        });

        await _context.SaveChangesAsync();
        return await GetCongregationByIdAsync(id);
    }

    /// <summary>
    /// Reconciles the congregation's associated pastors with the requested set:
    /// removes dropped links, adds new ones, and ensures at most one primary
    /// (falling back to the first association when none is flagged).
    /// </summary>
    private void SyncPastors(Congregation congregation, List<CongregationPastorInput> desired)
    {
        var deduped = desired
            .GroupBy(d => d.PreacherId)
            .Select(g => new CongregationPastorInput(g.Key, g.Any(x => x.IsPrimary)))
            .ToList();

        var desiredIds = deduped.Select(d => d.PreacherId).ToHashSet();

        var toRemove = congregation.Pastors.Where(cp => !desiredIds.Contains(cp.PreacherId)).ToList();
        foreach (var cp in toRemove)
        {
            congregation.Pastors.Remove(cp);
        }

        var primaryAssigned = false;
        foreach (var input in deduped)
        {
            var existing = congregation.Pastors.FirstOrDefault(cp => cp.PreacherId == input.PreacherId);
            var isPrimary = input.IsPrimary && !primaryAssigned;
            if (isPrimary) primaryAssigned = true;

            if (existing == null)
            {
                congregation.Pastors.Add(new CongregationPreacher
                {
                    CongregationId = congregation.Id,
                    PreacherId = input.PreacherId,
                    IsPrimary = isPrimary
                });
            }
            else
            {
                existing.IsPrimary = isPrimary;
            }
        }

        // Ensure a primary exists when there is at least one association.
        if (!primaryAssigned)
        {
            var first = congregation.Pastors.FirstOrDefault();
            if (first != null) first.IsPrimary = true;
        }
    }

    public async Task<DeleteOutcome> DeleteCongregationAsync(Guid id, string userId)
    {
        var congregation = await _context.Congregations.FindAsync(id);
        if (congregation == null)
        {
            return DeleteOutcome.NotFound;
        }

        if (await _context.Services.AnyAsync(s => s.CongregationId == id))
        {
            return DeleteOutcome.HasReferences;
        }

        var previous = System.Text.Json.JsonSerializer.Serialize(new
        {
            congregation.Name,
            congregation.City,
            congregation.DenominationId
        });

        _context.Congregations.Remove(congregation);
        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(Congregation),
            EntityId = id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Deleted,
            PreviousValues = previous
        });
        await _context.SaveChangesAsync();
        return DeleteOutcome.Deleted;
    }

    public async Task<List<CongregationSummaryDto>> SearchCongregationsAsync(string query)
    {
        return await _context.Congregations
            .Include(c => c.Denomination)
            .Where(c => c.Name.Contains(query) || c.City.Contains(query))
            .OrderBy(c => c.Name)
            .Take(10)
            .Select(c => new CongregationSummaryDto(
                c.Id,
                c.Name,
                c.City,
                c.Denomination != null ? c.Denomination.Abbreviation : null,
                c.DenominationId,
                c.Pastors
                    .Select(cp => new CongregationPastorDto(cp.PreacherId, cp.Preacher.FullName, cp.Preacher.City, cp.IsPrimary))
                    .ToList()))
            .ToListAsync();
    }
}
