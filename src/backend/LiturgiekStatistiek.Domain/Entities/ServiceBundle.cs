namespace LiturgiekStatistiek.Domain.Entities;

public class ServiceBundle
{
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public Guid BundleId { get; set; }
    public ListItem Bundle { get; set; } = null!;
}
