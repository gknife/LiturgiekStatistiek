namespace LiturgiekStatistiek.Domain.Entities;

public class ChangeHistory
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public ChangeType ChangeType { get; set; }
    public string? PreviousValues { get; set; }
}
