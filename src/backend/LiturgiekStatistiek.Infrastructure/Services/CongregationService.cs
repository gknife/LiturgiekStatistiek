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
                c.Longitude))
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
                c.Longitude))
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
        await _context.SaveChangesAsync();

        return await GetCongregationByIdAsync(congregation.Id) ?? throw new InvalidOperationException();
    }

    public async Task<CongregationDto?> UpdateCongregationAsync(Guid id, UpdateCongregationRequest request, string userId)
    {
        var congregation = await _context.Congregations.FindAsync(id);
        if (congregation == null)
        {
            return null;
        }

        congregation.Name = request.Name;
        congregation.City = request.City;
        congregation.LocationDetail = request.LocationDetail;
        congregation.DenominationId = request.DenominationId;
        congregation.Modality = request.Modality;
        congregation.Latitude = request.Latitude;
        congregation.Longitude = request.Longitude;
        congregation.ModifiedBy = userId;
        congregation.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetCongregationByIdAsync(id);
    }

    public async Task<List<CongregationSummaryDto>> SearchCongregationsAsync(string query)
    {
        return await _context.Congregations
            .Include(c => c.Denomination)
            .Where(c => c.Services.Any())
            .Where(c => c.Name.Contains(query) || c.City.Contains(query))
            .OrderBy(c => c.Name)
            .Take(10)
            .Select(c => new CongregationSummaryDto(
                c.Id,
                c.Name,
                c.City,
                c.Denomination != null ? c.Denomination.Abbreviation : null))
            .ToListAsync();
    }
}
