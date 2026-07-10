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
                    .OrderBy(i => i.Value)
                    .Select(i => new ListItemDto(i.Id, i.Value, i.Abbreviation, i.SortOrder, i.IsActive, (int?)i.LiturgicalElementType, i.CreatedBy, i.CreatedAt, i.ModifiedBy, i.ModifiedAt))
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
                    .OrderBy(i => i.Value)
                    .Select(i => new ListItemDto(i.Id, i.Value, i.Abbreviation, i.SortOrder, i.IsActive, (int?)i.LiturgicalElementType, i.CreatedBy, i.CreatedAt, i.ModifiedBy, i.ModifiedAt))
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
        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(ListItem),
            EntityId = item.Id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Created,
            PreviousValues = null
        });
        await _context.SaveChangesAsync();

        return new ListItemDto(item.Id, item.Value, item.Abbreviation, item.SortOrder, item.IsActive, (int?)item.LiturgicalElementType, item.CreatedBy, item.CreatedAt, item.ModifiedBy, item.ModifiedAt);
    }

    public async Task<ListItemDto?> UpdateListItemAsync(Guid id, UpdateListItemRequest request, string userId)
    {
        var item = await _context.ListItems.FindAsync(id);
        if (item == null)
        {
            return null;
        }

        var previous = System.Text.Json.JsonSerializer.Serialize(new
        {
            item.Value,
            item.Abbreviation,
            item.SortOrder,
            item.IsActive,
            LiturgicalElementType = (int?)item.LiturgicalElementType
        });

        item.Value = request.Value;
        item.Abbreviation = request.Abbreviation;
        item.SortOrder = request.SortOrder;
        item.IsActive = request.IsActive;
        item.LiturgicalElementType = request.LiturgicalElementType.HasValue
            ? (ElementType)request.LiturgicalElementType.Value
            : null;
        item.ModifiedBy = userId;
        item.ModifiedAt = DateTime.UtcNow;

        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(ListItem),
            EntityId = item.Id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Updated,
            PreviousValues = previous
        });

        await _context.SaveChangesAsync();
        return new ListItemDto(item.Id, item.Value, item.Abbreviation, item.SortOrder, item.IsActive, (int?)item.LiturgicalElementType, item.CreatedBy, item.CreatedAt, item.ModifiedBy, item.ModifiedAt);
    }

    public async Task<bool> DeleteListItemAsync(Guid id, string userId)
    {
        var item = await _context.ListItems.FindAsync(id);
        if (item == null)
        {
            return false;
        }

        var previous = System.Text.Json.JsonSerializer.Serialize(new
        {
            item.Value,
            item.Abbreviation,
            item.SortOrder,
            item.IsActive,
            LiturgicalElementType = (int?)item.LiturgicalElementType
        });

        _context.ListItems.Remove(item);
        _context.ChangeHistory.Add(new ChangeHistory
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(ListItem),
            EntityId = id,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            ChangeType = ChangeType.Deleted,
            PreviousValues = previous
        });
        await _context.SaveChangesAsync();
        return true;
    }
}
