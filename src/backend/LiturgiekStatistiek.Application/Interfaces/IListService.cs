using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IListService
{
    Task<List<ListDefinitionDto>> GetAllListsAsync();
    Task<ListDefinitionDto?> GetListByNameAsync(string name);
    Task<ListDefinitionDto> CreateListDefinitionAsync(CreateListDefinitionRequest request, string userId);
    Task<ListItemDto> AddListItemAsync(CreateListItemRequest request, string userId);
    Task<ListItemDto?> UpdateListItemAsync(Guid id, UpdateListItemRequest request, string userId);
    Task<bool> DeleteListItemAsync(Guid id);
}
