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
                p.Title,
                p.Denomination != null ? p.Denomination.Value : null,
                p.City))
            .ToListAsync();

        return new PaginatedResult<PreacherDto>(items, totalCount, page, pageSize);
    }

    public async Task<PreacherDto?> GetPreacherByIdAsync(Guid id)
    {
        return await _context.Preachers
            .Include(p => p.Denomination)
            .Where(p => p.Id == id)
            .Select(p => new PreacherDto(
                p.Id,
                p.FullName,
                p.Title,
                p.Denomination != null ? p.Denomination.Value : null,
                p.City))
            .FirstOrDefaultAsync();
    }

    public async Task<PreacherDto> CreatePreacherAsync(CreatePreacherRequest request, string userId)
    {
        var preacher = new Preacher
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Title = request.Title,
            DenominationId = request.DenominationId,
            City = request.City,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Preachers.Add(preacher);
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

        preacher.FullName = request.FullName;
        preacher.Title = request.Title;
        preacher.DenominationId = request.DenominationId;
        preacher.City = request.City;
        preacher.ModifiedBy = userId;
        preacher.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetPreacherByIdAsync(id);
    }

    public async Task<List<PreacherSummaryDto>> SearchPreachersAsync(string query)
    {
        return await _context.Preachers
            .Where(p => p.Services.Any())
            .Where(p => p.FullName.Contains(query))
            .OrderBy(p => p.FullName)
            .Take(10)
            .Select(p => new PreacherSummaryDto(p.Id, p.FullName, p.Title))
            .ToListAsync();
    }
}
