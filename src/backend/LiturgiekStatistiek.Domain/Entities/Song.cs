using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class Song : IHasAuditFields
{
    public Guid Id { get; set; }
    public Guid BundleId { get; set; }
    public ListItem Bundle { get; set; } = null!;
    public string Section { get; set; } = "";
    public int Number { get; set; }
    public string? Title { get; set; }
    public int? NumberOfVerses { get; set; }
    public ICollection<SongCatalogVerse> Verses { get; set; } = new List<SongCatalogVerse>();
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
