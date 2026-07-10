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

    /// <summary>
    /// Explicitly marks that the whole song (all verses) was sung. Set from the
    /// "Hele lied / alle verzen" checkbox. When true the song counts as complete
    /// regardless of whether the catalog verse count is known.
    /// </summary>
    public bool SungInFull { get; set; }

    public ICollection<SongVerse> Verses { get; set; } = new List<SongVerse>();
}
