using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// A rubriek/categorie defined for a specific liedbundel (a <see cref="ListItem"/> in the
/// "SongBundles" list). Drives the Categorie dropdown on the LiedCatalogus and in the dienst
/// editor. <see cref="Song.Section"/> stores the chosen value as a string; renaming a section
/// cascades to existing songs and service song references.
/// </summary>
public class BundleSection : IHasAuditFields
{
    public Guid Id { get; set; }

    /// <summary>The liedbundel (ListItem in the SongBundles list) this rubriek belongs to.</summary>
    public Guid BundleId { get; set; }
    public ListItem Bundle { get; set; } = null!;

    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>When true this rubriek is pre-selected in the Lied dropdown for its bundle.</summary>
    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
