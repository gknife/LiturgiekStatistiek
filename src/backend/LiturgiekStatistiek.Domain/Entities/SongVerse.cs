namespace LiturgiekStatistiek.Domain.Entities;

public class SongVerse
{
    public Guid Id { get; set; }
    public Guid ServiceElementSongId { get; set; }
    public ServiceElementSong ServiceElementSong { get; set; } = null!;
    public string VerseLabel { get; set; } = string.Empty;
    public int Position { get; set; }
}
