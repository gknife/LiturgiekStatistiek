using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Update-flow tests backed by a real relational provider (SQLite). Unlike the
/// EF InMemory provider, SQLite enforces foreign keys and ON DELETE CASCADE, so
/// these tests exercise the cascade behaviour that only manifests on SQL Server
/// in production (the edit/update flow deletes and rebuilds the element graph).
/// </summary>
[TestFixture]
public class ServiceServiceUpdateIntegrationTests
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _db = null!;
    private ServiceService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Seed with a separate context so the seeded graph is not left tracked.
        // Production uses a fresh DbContext per request; sharing one context here
        // would mask the cascade behaviour under test.
        await using (var seedContext = new ApplicationDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(seedContext);
        }

        _db = new ApplicationDbContext(options);
        _sut = new ServiceService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task UpdateServiceAsync_ReplacingElementGraph_DoesNotThrowAndPersists()
    {
        // A seeded service that has a full Elements -> Songs -> Verses graph.
        var service = await _db.Services
            .Include(s => s.Elements)
                .ThenInclude(e => e.Songs)
                    .ThenInclude(sg => sg.Verses)
            .AsNoTracking()
            .FirstAsync(s => s.Elements.Any(e => e.Songs.Any(sg => sg.Verses.Any())));

        var id = service.Id;
        var bundleId = service.Elements.SelectMany(e => e.Songs).First().BundleId;

        var request = BuildRequest(service, TimeOfDay.Evening, new List<CreateServiceElementRequest>
        {
            new(
                Position: 1,
                ElementType: 0,
                LabelId: null,
                ScriptureReference: null,
                Notes: "Vervangen onderdeel",
                Songs: new List<CreateServiceElementSongRequest>
                {
                    new(BundleId: bundleId, Section: null, SongNumber: 42, Position: 1, Verses: new List<string> { "1", "2" })
                })
        });

        var result = await _sut.UpdateServiceAsync(id, request, "tester");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TimeOfDayValue, Is.EqualTo((int)TimeOfDay.Evening));

        var reloaded = await _db.Services
            .Include(s => s.Elements)
                .ThenInclude(e => e.Songs)
                    .ThenInclude(sg => sg.Verses)
            .AsNoTracking()
            .FirstAsync(s => s.Id == id);

        Assert.That(reloaded.Elements, Has.Count.EqualTo(1));
        var song = reloaded.Elements.Single().Songs.Single();
        Assert.That(song.SongNumber, Is.EqualTo(42));
        Assert.That(song.Verses.Select(v => v.VerseLabel), Is.EquivalentTo(new[] { "1", "2" }));
    }

    [Test]
    public async Task UpdateServiceAsync_PersistsSungInFull_AndReportsCompleteness()
    {
        var service = await _db.Services
            .Include(s => s.Elements)
                .ThenInclude(e => e.Songs)
                    .ThenInclude(sg => sg.Verses)
            .AsNoTracking()
            .FirstAsync(s => s.Elements.Any(e => e.Songs.Any(sg => sg.Verses.Any())));

        var id = service.Id;
        var bundleId = service.Elements.SelectMany(e => e.Songs).First().BundleId;

        var request = BuildRequest(service, TimeOfDay.Morning, new List<CreateServiceElementRequest>
        {
            new(
                Position: 1,
                ElementType: 0,
                LabelId: null,
                ScriptureReference: null,
                Notes: null,
                Songs: new List<CreateServiceElementSongRequest>
                {
                    new(BundleId: bundleId, Section: null, SongNumber: 42, Position: 1,
                        Verses: new List<string> { "1" }, SungInFull: true)
                })
        });

        var result = await _sut.UpdateServiceAsync(id, request, "tester");
        Assert.That(result, Is.Not.Null);

        var reloaded = await _db.Services
            .Include(s => s.Elements)
                .ThenInclude(e => e.Songs)
            .AsNoTracking()
            .FirstAsync(s => s.Id == id);

        var song = reloaded.Elements.Single().Songs.Single();
        Assert.That(song.SungInFull, Is.True,
            "The explicit 'hele lied' flag must be persisted on the song.");

        var dto = await _sut.GetServiceByIdAsync(id);
        var songDto = dto!.Elements.Single().Songs.Single();
        Assert.Multiple(() =>
        {
            Assert.That(songDto.SungInFull, Is.True);
            Assert.That(songDto.Completeness, Is.Not.Null);
            Assert.That(songDto.Completeness!.CompleteInElement, Is.True,
                "A song marked 'hele lied' is complete regardless of the catalog verse count.");
        });
    }

    [Test]
    public async Task UpdateServiceAsync_AddingSermonReferences_PersistsThem()
    {
        // Regression: updating a service with sermon-text references used to throw
        // DbUpdateConcurrencyException because the refs were re-added through the
        // tracked navigation instead of the DbSet after the delete flush.
        var service = await _db.Services.AsNoTracking().FirstAsync();
        var id = service.Id;
        var book = await _db.BibleBooks.AsNoTracking().FirstAsync();

        var request = BuildRequest(service, TimeOfDay.Morning,
            new List<CreateServiceElementRequest>(),
            new List<SermonTextReferenceRequest>
            {
                new(BibleBookId: book.Id, BookName: book.Name, Chapter: 3, VerseStart: 16, VerseEnd: 17, Position: 0)
            });

        var result = await _sut.UpdateServiceAsync(id, request, "tester");
        Assert.That(result, Is.Not.Null);

        var reloaded = await _db.Services
            .Include(s => s.SermonTextReferences)
            .AsNoTracking()
            .FirstAsync(s => s.Id == id);

        Assert.That(reloaded.SermonTextReferences, Has.Count.EqualTo(1));
        var reference = reloaded.SermonTextReferences.Single();
        Assert.Multiple(() =>
        {
            Assert.That(reference.BibleBookId, Is.EqualTo(book.Id));
            Assert.That(reference.Chapter, Is.EqualTo(3));
            Assert.That(reference.VerseStart, Is.EqualTo(16));
            Assert.That(reference.VerseEnd, Is.EqualTo(17));
        });
    }

    [Test]
    public async Task UpdateServiceAsync_ReplacingSermonReferences_ReplacesNotAppends()
    {
        var service = await _db.Services.AsNoTracking().FirstAsync();
        var id = service.Id;
        var book = await _db.BibleBooks.AsNoTracking().FirstAsync();

        // First update: one reference.
        await _sut.UpdateServiceAsync(id, BuildRequest(service, TimeOfDay.Morning,
            new List<CreateServiceElementRequest>(),
            new List<SermonTextReferenceRequest>
            {
                new(BibleBookId: book.Id, BookName: book.Name, Chapter: 1, VerseStart: 1, VerseEnd: 2, Position: 0)
            }), "tester");

        // Second update: a different single reference must replace, not append.
        await _sut.UpdateServiceAsync(id, BuildRequest(service, TimeOfDay.Morning,
            new List<CreateServiceElementRequest>(),
            new List<SermonTextReferenceRequest>
            {
                new(BibleBookId: book.Id, BookName: book.Name, Chapter: 5, VerseStart: 3, VerseEnd: 4, Position: 0)
            }), "tester");

        var reloaded = await _db.Services
            .Include(s => s.SermonTextReferences)
            .AsNoTracking()
            .FirstAsync(s => s.Id == id);

        Assert.That(reloaded.SermonTextReferences, Has.Count.EqualTo(1));
        Assert.That(reloaded.SermonTextReferences.Single().Chapter, Is.EqualTo(5));
    }

    [Test]
    public async Task UpdateServiceAsync_WithSermonRefAndReadingElement_PersistsBoth()
    {
        // The two originally reported bugs (preektekst + schriftlezing not saving)
        // shared this path: a PUT carrying both a sermon ref and a reading element.
        var service = await _db.Services.AsNoTracking().FirstAsync();
        var id = service.Id;
        var book = await _db.BibleBooks.AsNoTracking().FirstAsync();

        var request = BuildRequest(service, TimeOfDay.Morning,
            new List<CreateServiceElementRequest>
            {
                new(
                    Position: 1,
                    ElementType: 2,
                    LabelId: null,
                    ScriptureReference: null,
                    Notes: null,
                    Songs: new List<CreateServiceElementSongRequest>(),
                    ReadingReferences: new List<ReadingReferenceRequest>
                    {
                        new(BibleBookId: book.Id, BookName: book.Name, Chapter: 1, VerseStart: 1, VerseEnd: 5, Position: 0)
                    })
            },
            new List<SermonTextReferenceRequest>
            {
                new(BibleBookId: book.Id, BookName: book.Name, Chapter: 4, VerseStart: 1, VerseEnd: 2, Position: 0)
            });

        var result = await _sut.UpdateServiceAsync(id, request, "tester");
        Assert.That(result, Is.Not.Null);

        var reloaded = await _db.Services
            .Include(s => s.SermonTextReferences)
            .Include(s => s.Elements)
                .ThenInclude(e => e.ReadingReferences)
            .AsNoTracking()
            .FirstAsync(s => s.Id == id);

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.SermonTextReferences, Has.Count.EqualTo(1),
                "The preektekst reference must persist.");
            Assert.That(reloaded.Elements, Has.Count.EqualTo(1));
            Assert.That(reloaded.Elements.Single().ReadingReferences, Has.Count.EqualTo(1),
                "The schriftlezing reference must persist alongside the preektekst.");
        });
    }

    private static UpdateServiceRequest BuildRequest(Service s, TimeOfDay timeOfDay, List<CreateServiceElementRequest> elements) =>
        BuildRequest(s, timeOfDay, elements, null);

    private static UpdateServiceRequest BuildRequest(Service s, TimeOfDay timeOfDay, List<CreateServiceElementRequest> elements, List<SermonTextReferenceRequest>? sermonReferences) =>
        new(
            Date: s.Date,
            TimeOfDay: (int)timeOfDay,
            CongregationId: s.CongregationId,
            PreacherId: s.PreacherId,
            ChurchCalendarSundayId: s.ChurchCalendarSundayId,
            IsReadingService: s.IsReadingService,
            ReadSermonBy: s.ReadSermonBy,
            MusicalAccompanimentId: s.MusicalAccompanimentId,
            HasBeamerLiturgy: s.HasBeamerLiturgy,
            HasBeamerTexts: s.HasBeamerTexts,
            HasBeamerSongs: s.HasBeamerSongs,
            HasBeamerTextsAndSongs: s.HasBeamerTextsAndSongs,
            BroadcastUrl: s.BroadcastUrl,
            SpecialOccasionId: s.SpecialOccasionId,
            SermonText: s.SermonText,
            SermonTheme: s.SermonTheme,
            Notes: s.Notes,
            BundleIds: null,
            Elements: elements,
            SermonTextReferences: sermonReferences);
}
