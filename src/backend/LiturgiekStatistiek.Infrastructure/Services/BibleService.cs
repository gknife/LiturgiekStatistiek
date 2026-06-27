using System.Text.Json;
using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class BibleService : IBibleService
{
    private readonly IApplicationDbContext _db;

    public BibleService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<BibleBookDto>> GetBooksAsync(string? translationAbbreviation = null, CancellationToken ct = default)
    {
        var books = await _db.BibleBooks
            .AsNoTracking()
            .Include(b => b.TranslationNames)
            .OrderBy(b => b.Ordinal)
            .ToListAsync(ct);

        return books.Select(b =>
        {
            var name = b.Name;
            if (!string.IsNullOrWhiteSpace(translationAbbreviation))
            {
                var match = b.TranslationNames
                    .FirstOrDefault(t => t.TranslationAbbreviation == translationAbbreviation);
                if (match != null && !string.IsNullOrWhiteSpace(match.Name))
                {
                    name = match.Name;
                }
            }

            var verseCounts = JsonSerializer.Deserialize<List<int>>(b.VerseCountsJson) ?? new List<int>();
            return new BibleBookDto(b.Id, b.Ordinal, b.Testament, name, b.ChapterCount, verseCounts);
        }).ToList();
    }
}
