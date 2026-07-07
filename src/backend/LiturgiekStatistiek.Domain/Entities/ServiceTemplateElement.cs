namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// A single onderdeel in a <see cref="ServiceTemplate"/>. Carries the label/type
/// plus optional defaults (performer, beurtzang, a fixed scripture reference for
/// standing readings). Week-specific content (songs, sermon text) is never stored
/// on a template.
/// </summary>
public class ServiceTemplateElement
{
    public Guid Id { get; set; }
    public Guid ServiceTemplateId { get; set; }
    public ServiceTemplate ServiceTemplate { get; set; } = null!;

    public int Position { get; set; }
    public ElementType ElementType { get; set; }

    public Guid? LabelId { get; set; }
    public ListItem? Label { get; set; }

    /// <summary>Default performer (ListItem in the ServicePerformer list) for textual onderdelen.</summary>
    public Guid? PerformerId { get; set; }
    public ListItem? Performer { get; set; }

    /// <summary>Default beurtzang flag for song onderdelen.</summary>
    public bool IsBeurtzang { get; set; }

    /// <summary>Optional fixed scripture reference for standing readings (e.g. Wet des Heren).</summary>
    public string? FixedScriptureReference { get; set; }
}
