namespace LiturgiekStatistiek.Domain.Entities;

public class ServiceElementSong
{
    public Guid Id { get; set; }
    public Guid ServiceElementId { get; set; }
    public ServiceElement ServiceElement { get; set; } = null!;
    public Guid BundleId { get; set; }
    public ListItem Bundle { get; set; } = null!;
    public string Section { get; set; } = "";
    public int SongNumber { get; set; }
    public int Position { get; set; }

    public ICollection<SongVerse> Verses { get; set; } = new List<SongVerse>();
}
