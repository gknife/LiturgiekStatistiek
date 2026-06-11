using LiturgiekStatistiek.Domain.Interfaces;

namespace LiturgiekStatistiek.Domain.Entities;

public class ServiceElement : IHasAuditFields
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int Position { get; set; }
    public ElementType ElementType { get; set; }
    public Guid? LabelId { get; set; }
    public ListItem? Label { get; set; }
    public string? ScriptureReference { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public ICollection<ServiceElementSong> Songs { get; set; } = new List<ServiceElementSong>();
}
