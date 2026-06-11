using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class ListDefinition : IHasAuditFields
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemList { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<ListItem> Items { get; set; } = new List<ListItem>();
}
