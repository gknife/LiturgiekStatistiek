using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Template CRUD, best-match resolution (specificity scoring) and instantiation
/// into ready-to-use onderdelen. Backed by SQLite for realistic relational behaviour.
/// </summary>
[TestFixture]
public class TemplateServiceIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private ApplicationDbContext _db = null!;
    private TemplateService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using (var seedContext = new ApplicationDbContext(_options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(seedContext);
        }

        _db = new ApplicationDbContext(_options);
        _sut = new TemplateService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task CreateThenInstantiate_ReturnsTemplateElementsAsServiceElements()
    {
        var congregationId = await FirstCongregationIdAsync();
        var labelId = await FirstLabelIdAsync();

        var request = new CreateServiceTemplateRequest(
            Name: "ZZ-TEST Ochtenddienst",
            DenominationId: null,
            CongregationId: congregationId,
            TimeOfDay: (int)TimeOfDay.Morning,
            OccasionId: null,
            IsActive: true,
            Elements: new()
            {
                new(Position: 1, ElementType: 0, LabelId: labelId, PerformerId: null, IsBeurtzang: false, FixedScriptureReference: null),
                new(Position: 2, ElementType: 2, LabelId: labelId, PerformerId: null, IsBeurtzang: false, FixedScriptureReference: "Genesis 1"),
            });

        var created = await _sut.CreateTemplateAsync(request, "tester");
        Assert.That(created.Elements, Has.Count.EqualTo(2));

        var instantiated = await _sut.InstantiateAsync(null, congregationId, (int)TimeOfDay.Morning, null);
        Assert.That(instantiated, Is.Not.Null);
        Assert.That(instantiated!, Has.Count.EqualTo(2));
        Assert.That(instantiated[0].Position, Is.EqualTo(1));
    }

    [Test]
    public async Task Resolve_PrefersCongregationSpecificTemplateOverDenominationOnly()
    {
        var congregationId = await FirstCongregationIdAsync();
        var denominationId = await FirstDenominationIdAsync();

        // A generic denomination-level template.
        await _sut.CreateTemplateAsync(new CreateServiceTemplateRequest(
            "ZZ-TEST Denominatie-sjabloon", denominationId, null, null, null, true,
            new() { new(1, 0, null, null, false, null) }), "tester");

        // A more specific congregation-level template.
        var specific = await _sut.CreateTemplateAsync(new CreateServiceTemplateRequest(
            "ZZ-TEST Gemeente-sjabloon", denominationId, congregationId, null, null, true,
            new() { new(1, 0, null, null, false, null) }), "tester");

        var resolved = await _sut.ResolveAsync(denominationId, congregationId, null, null);

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.Id, Is.EqualTo(specific.Id),
            "The congregation-specific template must win over the denomination-only one.");
    }

    [Test]
    public async Task Create_PersistsDefaultCharacteristics_AndReturnsThemOnGet()
    {
        var congregationId = await FirstCongregationIdAsync();
        var translationId = await FirstBibleTranslationIdAsync();
        var accompanimentId = await FirstMusicalAccompanimentIdAsync();
        var bundleId = await FirstSongBundleIdAsync();

        var created = await _sut.CreateTemplateAsync(new CreateServiceTemplateRequest(
            Name: "ZZ-TEST Kenmerken",
            DenominationId: null,
            CongregationId: congregationId,
            TimeOfDay: (int)TimeOfDay.Morning,
            OccasionId: null,
            IsActive: true,
            Elements: new() { new(1, 0, null, null, false, null) },
            MusicalAccompanimentId: accompanimentId,
            IsReadingService: true,
            HasBeamerLiturgy: true,
            HasBeamerTexts: false,
            HasBeamerSongs: true,
            DefaultBibleTranslationId: translationId,
            DefaultSongBundleId: bundleId), "tester");

        var fetched = await _sut.GetTemplateByIdAsync(created.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(fetched!.MusicalAccompanimentId, Is.EqualTo(accompanimentId));
            Assert.That(fetched.DefaultBibleTranslationId, Is.EqualTo(translationId));
            Assert.That(fetched.DefaultSongBundleId, Is.EqualTo(bundleId));
            Assert.That(fetched.IsReadingService, Is.True);
            Assert.That(fetched.HasBeamerLiturgy, Is.True);
            Assert.That(fetched.HasBeamerTexts, Is.False);
            Assert.That(fetched.HasBeamerSongs, Is.True);
            Assert.That(fetched.MusicalAccompaniment, Is.Not.Null.And.Not.Empty);
            Assert.That(fetched.DefaultBibleTranslation, Is.Not.Null.And.Not.Empty);
            Assert.That(fetched.DefaultSongBundle, Is.Not.Null.And.Not.Empty);
        });
    }

    private async Task<Guid> FirstSongBundleIdAsync() =>
        (await _db.ListItems.AsNoTracking().FirstAsync(i => i.ListDefinition.Name == "SongBundles")).Id;

    private async Task<Guid> FirstCongregationIdAsync() =>
        (await _db.Congregations.AsNoTracking().FirstAsync()).Id;

    private async Task<Guid> FirstLabelIdAsync() =>
        (await _db.ListItems.AsNoTracking().FirstAsync(i => i.ListDefinition.Name == "LiturgicalLabels")).Id;

    private async Task<Guid> FirstDenominationIdAsync() =>
        (await _db.ListItems.AsNoTracking().FirstAsync(i => i.ListDefinition.Name == "Denominations")).Id;

    private async Task<Guid> FirstBibleTranslationIdAsync() =>
        (await _db.ListItems.AsNoTracking().FirstAsync(i => i.ListDefinition.Name == "BibleTranslations")).Id;

    private async Task<Guid> FirstMusicalAccompanimentIdAsync() =>
        (await _db.ListItems.AsNoTracking().FirstAsync(i => i.ListDefinition.Name == "MusicalAccompaniment")).Id;
}
