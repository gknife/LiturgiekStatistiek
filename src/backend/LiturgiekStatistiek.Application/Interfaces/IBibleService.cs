using LiturgiekStatistiek.Application.DTOs;

namespace LiturgiekStatistiek.Application.Interfaces;

public interface IBibleService
{
    /// <summary>
    /// Returns the 66 canonical books in order. When <paramref name="translationAbbreviation"/>
    /// is supplied and a translation-specific name exists, it overrides the canonical name.
    /// </summary>
    Task<List<BibleBookDto>> GetBooksAsync(string? translationAbbreviation = null, CancellationToken ct = default);
}
