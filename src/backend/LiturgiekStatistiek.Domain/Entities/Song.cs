using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class Song : IHasAuditFields
{
    public Guid Id { get; set; }
    public Guid BundleId { get; set; }
    public ListItem Bundle { get; set; } = null!;
    public int Number { get; set; }
    public string? Title { get; set; }
    public int? NumberOfVerses { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
