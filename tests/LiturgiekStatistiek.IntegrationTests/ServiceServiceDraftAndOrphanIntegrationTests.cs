using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Draft/publish lifecycle and reference-entity retention behaviour, backed by SQLite
/// so that foreign-key cascade and the "curated gemeente/voorganger are retained even
/// with no remaining services" rule are exercised the same way as on SQL Server.
/// </summary>
[TestFixture]
public class ServiceServiceDraftAndOrphanIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private ApplicationDbContext _db = null!;
    private ServiceService _sut = null!;

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
        _sut = new ServiceService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task CreateServiceAsync_WithStatusConcept_IsExcludedFromDefaultQueryAndThenPublishedIncluded()
    {
        var congregationId = await NewCongregationAsync("ZZ-TEST Concept Gemeente");
        var request = BuildCreateRequest(congregationId, status: (int)ServiceStatus.Concept);

        var created = await _sut.CreateServiceAsync(request, "tester");

        // Concept is hidden when includeConcepts = false.
        var published = await _sut.GetServicesAsync(includeConcepts: false);
        Assert.That(published.Items.Any(s => s.Id == created.Id), Is.False,
            "A concept service must be excluded when concepts are not requested.");

        // Concept is visible when includeConcepts = true.
        var all = await _sut.GetServicesAsync(includeConcepts: true);
        Assert.That(all.Items.Any(s => s.Id == created.Id), Is.True);

        // Publishing flips the status and makes it visible in the published view.
        var publishedDto = await _sut.PublishServiceAsync(created.Id, "tester");
        Assert.That(publishedDto, Is.Not.Null);
        Assert.That(publishedDto!.StatusValue, Is.EqualTo((int)ServiceStatus.Gepubliceerd));

        var afterPublish = await _sut.GetServicesAsync(includeConcepts: false);
        Assert.That(afterPublish.Items.Any(s => s.Id == created.Id), Is.True);
    }

    [Test]
    public async Task DeleteServiceAsync_RetainsCongregationThatHasNoServicesLeft()
    {
        var congregationId = await NewCongregationAsync("ZZ-TEST Orphan Gemeente");
        var created = await _sut.CreateServiceAsync(BuildCreateRequest(congregationId), "tester");

        var deleted = await _sut.DeleteServiceAsync(created.Id);
        Assert.That(deleted, Is.True);

        await using var verify = new ApplicationDbContext(_options);
        var stillThere = await verify.Congregations.AnyAsync(c => c.Id == congregationId);
        Assert.That(stillThere, Is.True,
            "A curated gemeente must be retained even when it has no remaining services.");
    }

    private async Task<Guid> NewCongregationAsync(string name)
    {
        var congregation = new Congregation { Id = Guid.NewGuid(), Name = name, City = "ZZ-TEST Stad" };
        _db.Congregations.Add(congregation);
        await _db.SaveChangesAsync();
        return congregation.Id;
    }

    private static CreateServiceRequest BuildCreateRequest(Guid congregationId, int? status = null) =>
        new(
            Date: new DateOnly(2026, 1, 4),
            TimeOfDay: (int)TimeOfDay.Morning,
            CongregationId: congregationId,
            PreacherId: null,
            ChurchCalendarSundayId: null,
            IsReadingService: false,
            ReadSermonBy: null,
            MusicalAccompanimentId: null,
            HasBeamerLiturgy: false,
            HasBeamerTexts: false,
            HasBeamerSongs: false,
            HasBeamerTextsAndSongs: false,
            BroadcastUrl: null,
            SpecialOccasionId: null,
            SermonText: null,
            SermonTheme: null,
            Notes: "ZZ-TEST",
            BundleIds: null,
            Elements: null,
            SermonTextReferences: null,
            Status: status);
}
