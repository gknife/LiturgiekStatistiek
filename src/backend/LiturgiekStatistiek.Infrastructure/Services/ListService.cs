using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class ListService : IListService
{
    private readonly ApplicationDbContext _context;

    public ListService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ListDefinitionDto>> GetAllListsAsync()
    {
        return await _context.ListDefinitions
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new ListDefinitionDto(
                d.Id,
                d.Name,
                d.Description,
                d.IsSystemList,
                d.Items
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ListItemDto(i.Id, i.Value, i.Abbreviation, i.SortOrder, i.IsActive))
                    .ToList()))
            .ToListAsync();
    }

    public async Task<ListDefinitionDto?> GetListByNameAsync(string name)
    {
        return await _context.ListDefinitions
            .AsNoTracking()
            .Where(d => d.Name == name)
            .Select(d => new ListDefinitionDto(
                d.Id,
                d.Name,
                d.Description,
                d.IsSystemList,
                d.Items
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ListItemDto(i.Id, i.Value, i.Abbreviation, i.SortOrder, i.IsActive))
                    .ToList()))
            .FirstOrDefaultAsync();
    }

    public async Task<ListDefinitionDto> CreateListDefinitionAsync(CreateListDefinitionRequest request, string userId)
    {
        var definition = new ListDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsSystemList = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ListDefinitions.Add(definition);
        await _context.SaveChangesAsync();

        return new ListDefinitionDto(definition.Id, definition.Name, definition.Description, definition.IsSystemList, new List<ListItemDto>());
    }

    public async Task<ListItemDto> AddListItemAsync(CreateListItemRequest request, string userId)
    {
        var item = new ListItem
        {
            Id = Guid.NewGuid(),
            ListDefinitionId = request.ListDefinitionId,
            Value = request.Value,
            Abbreviation = request.Abbreviation,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ListItems.Add(item);
        await _context.SaveChangesAsync();

        return new ListItemDto(item.Id, item.Value, item.Abbreviation, item.SortOrder, item.IsActive);
    }

    public async Task<ListItemDto?> UpdateListItemAsync(Guid id, UpdateListItemRequest request, string userId)
    {
        var item = await _context.ListItems.FindAsync(id);
        if (item == null)
        {
            return null;
        }

        item.Value = request.Value;
        item.Abbreviation = request.Abbreviation;
        item.SortOrder = request.SortOrder;
        item.IsActive = request.IsActive;
        item.ModifiedBy = userId;
        item.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return new ListItemDto(item.Id, item.Value, item.Abbreviation, item.SortOrder, item.IsActive);
    }

    public async Task<bool> DeleteListItemAsync(Guid id)
    {
        var item = await _context.ListItems.FindAsync(id);
        if (item == null)
        {
            return false;
        }

        _context.ListItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }
}
