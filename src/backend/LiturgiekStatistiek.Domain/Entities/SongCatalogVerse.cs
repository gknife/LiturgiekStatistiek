namespace LiturgiekStatistiek.Domain.Entities;

public class SongCatalogVerse
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Song Song { get; set; } = null!;
    public int Number { get; set; }
    public string? Title { get; set; }
}
