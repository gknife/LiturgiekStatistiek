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

    /// <summary>
    /// Optional liturgical element classification. Only meaningful for items in
    /// the "LiturgicalLabels" list; used to filter the Onderdeel dropdown by the
    /// selected Type in the Add-dienst dialog.
    /// </summary>
    public ElementType? LiturgicalElementType { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
