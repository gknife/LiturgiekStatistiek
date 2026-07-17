using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Song-catalog behaviour backed by SQLite: named verses (e.g. a "Voorzang"
/// before verse 1) must be excluded from the verse count, and per-bundle
/// rubrieken (categorieën) support CRUD, single-default enforcement and a
/// rename that cascades to both catalog songs and service song references.
/// </summary>
[TestFixture]
public class SongServiceIntegrationTests
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _db = null!;
    private SongService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(seedContext);
        }

        _db = new ApplicationDbContext(options);
        _sut = new SongService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task CreateSong_WithNamedVerse_ExcludesLabeledVerseFromCount()
    {
        var bundleId = await FirstBundleIdAsync();

        var created = await _sut.CreateSongAsync(new CreateSongRequest(
            BundleId: bundleId,
            Section: "Psalm",
            Number: 918,
            Title: "ZZ-TEST met voorzang",
            NumberOfVerses: null,
            Verses: new List<SongVerseDto>
            {
                new(Number: 0, Title: null, Label: "Voorzang", SortOrder: 0),
                new(Number: 1, Title: null, Label: null, SortOrder: 1),
                new(Number: 2, Title: null, Label: null, SortOrder: 2),
                new(Number: 3, Title: null, Label: null, SortOrder: 3),
            }), "tester");

        Assert.That(created.NumberOfVerses, Is.EqualTo(3),
            "The labelled 'Voorzang' must not count towards the numbered verse total.");

        var fetched = await _sut.GetSongByIdAsync(created.Id);
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Verses, Has.Count.EqualTo(4));
        // Verses are ordered by SortOrder, so the named verse comes first.
        Assert.That(fetched.Verses![0].Label, Is.EqualTo("Voorzang"));
        Assert.That(fetched.Verses.Skip(1).All(v => v.Label == null), Is.True);
    }

    [Test]
    public async Task CreateSection_WithIsDefault_ClearsOtherDefaults()
    {
        var bundleId = await FirstBundleIdAsync();

        var first = await _sut.CreateSectionAsync(bundleId,
            new CreateBundleSectionRequest(Value: "ZZ-Rubriek A", SortOrder: 1, IsDefault: true), "tester");
        var second = await _sut.CreateSectionAsync(bundleId,
            new CreateBundleSectionRequest(Value: "ZZ-Rubriek B", SortOrder: 2, IsDefault: true), "tester");

        var sections = await _sut.GetSectionsAsync(bundleId);
        var a = sections.Single(s => s.Id == first.Id);
        var b = sections.Single(s => s.Id == second.Id);

        Assert.Multiple(() =>
        {
            Assert.That(b.IsDefault, Is.True, "The most recently defaulted rubriek stays default.");
            Assert.That(a.IsDefault, Is.False, "Only one rubriek per bundle may be the default.");
            Assert.That(sections.Count(s => s.IsDefault), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task UpdateSection_Rename_CascadesToCatalogSongsAndServiceSongReferences()
    {
        var bundleId = await FirstBundleIdAsync();
        const string oldValue = "ZZ-OudeRubriek";
        const string newValue = "ZZ-NieuweRubriek";

        // A catalog song filed under the old rubriek name.
        var song = new Song
        {
            Id = Guid.NewGuid(),
            BundleId = bundleId,
            Section = oldValue,
            Number = 917,
            Title = "ZZ-TEST cascade",
            CreatedBy = "tester",
            CreatedAt = DateTime.UtcNow
        };
        _db.Songs.Add(song);

        // An existing service song reference under the same bundle + rubriek.
        var serviceSong = await _db.ServiceElementSongs.FirstAsync();
        serviceSong.BundleId = bundleId;
        serviceSong.Section = oldValue;

        await _db.SaveChangesAsync();

        var section = await _sut.CreateSectionAsync(bundleId,
            new CreateBundleSectionRequest(Value: oldValue, SortOrder: 5, IsDefault: false), "tester");

        var updated = await _sut.UpdateSectionAsync(section.Id,
            new UpdateBundleSectionRequest(Value: newValue, SortOrder: 5, IsDefault: false, IsActive: true), "tester");

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Value, Is.EqualTo(newValue));

        var reloadedSong = await _db.Songs.AsNoTracking().FirstAsync(s => s.Id == song.Id);
        var reloadedServiceSong = await _db.ServiceElementSongs.AsNoTracking().FirstAsync(s => s.Id == serviceSong.Id);

        Assert.Multiple(() =>
        {
            Assert.That(reloadedSong.Section, Is.EqualTo(newValue),
                "Renaming a rubriek must cascade to catalog songs.");
            Assert.That(reloadedServiceSong.Section, Is.EqualTo(newValue),
                "Renaming a rubriek must cascade to service song references.");
        });
    }

    [Test]
    public async Task DeleteSection_RemovesIt()
    {
        var bundleId = await FirstBundleIdAsync();
        var section = await _sut.CreateSectionAsync(bundleId,
            new CreateBundleSectionRequest(Value: "ZZ-TeVerwijderen", SortOrder: 9, IsDefault: false), "tester");

        var deleted = await _sut.DeleteSectionAsync(section.Id);
        Assert.That(deleted, Is.True);

        var sections = await _sut.GetSectionsAsync(bundleId);
        Assert.That(sections.Any(s => s.Id == section.Id), Is.False);
    }

    private async Task<Guid> FirstBundleIdAsync() =>
        (await _db.ListItems.AsNoTracking().FirstAsync(i => i.ListDefinition.Name == "SongBundles")).Id;
}
