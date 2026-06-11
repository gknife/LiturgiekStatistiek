using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class ListItem : IHasAuditFields
{
    public Guid Id { get; set; }
    public Guid ListDefinitionId { get; set; }
    public ListDefinition ListDefinition { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
