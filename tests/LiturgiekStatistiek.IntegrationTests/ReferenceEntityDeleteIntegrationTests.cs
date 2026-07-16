using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

/// <summary>
/// Verifies the curated gemeente/voorganger delete rules: hard-delete only when there
/// are no referencing services, otherwise a <see cref="DeleteOutcome.HasReferences"/>
/// guard. Also confirms search surfaces records that have zero services.
/// </summary>
[TestFixture]
public class ReferenceEntityDeleteIntegrationTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private ApplicationDbContext _db = null!;
    private CongregationService _congregations = null!;
    private PreacherService _preachers = null!;
    private ServiceService _services = null!;

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
        _congregations = new CongregationService(_db);
        _preachers = new PreacherService(_db);
        _services = new ServiceService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task DeleteCongregationAsync_WithoutServices_HardDeletes()
    {
        var created = await _congregations.CreateCongregationAsync(
            new CreateCongregationRequest("ZZ-TEST Gemeente", "ZZ-Stad", null, null, null, null, null),
            "tester");

        var outcome = await _congregations.DeleteCongregationAsync(created.Id, "tester");

        Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
        Assert.That(await _db.Congregations.AnyAsync(c => c.Id == created.Id), Is.False);
    }

    [Test]
    public async Task DeleteCongregationAsync_WithServices_ReturnsHasReferences()
    {
        var created = await _congregations.CreateCongregationAsync(
            new CreateCongregationRequest("ZZ-TEST Gemeente", "ZZ-Stad", null, null, null, null, null),
            "tester");
        await _services.CreateServiceAsync(BuildServiceRequest(created.Id), "tester");

        var outcome = await _congregations.DeleteCongregationAsync(created.Id, "tester");

        Assert.That(outcome, Is.EqualTo(DeleteOutcome.HasReferences));
        Assert.That(await _db.Congregations.AnyAsync(c => c.Id == created.Id), Is.True);
    }

    [Test]
    public async Task DeletePreacherAsync_WithServices_ReturnsHasReferences()
    {
        var congregation = await _congregations.CreateCongregationAsync(
            new CreateCongregationRequest("ZZ-TEST Gemeente", "ZZ-Stad", null, null, null, null, null),
            "tester");
        var preacher = await _preachers.CreatePreacherAsync(
            new CreatePreacherRequest("Ds. ZZ Test", null, "ZZ-Stad"), "tester");
        await _services.CreateServiceAsync(BuildServiceRequest(congregation.Id, preacher.Id), "tester");

        var outcome = await _preachers.DeletePreacherAsync(preacher.Id, "tester");

        Assert.That(outcome, Is.EqualTo(DeleteOutcome.HasReferences));
    }

    [Test]
    public async Task DeletePreacherAsync_WithoutServices_HardDeletes()
    {
        var preacher = await _preachers.CreatePreacherAsync(
            new CreatePreacherRequest("Ds. ZZ Test", null, "ZZ-Stad"), "tester");

        var outcome = await _preachers.DeletePreacherAsync(preacher.Id, "tester");

        Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
        Assert.That(await _db.Preachers.AnyAsync(p => p.Id == preacher.Id), Is.False);
    }

    [Test]
    public async Task SearchCongregationsAsync_ReturnsRecordsWithoutServices()
    {
        var created = await _congregations.CreateCongregationAsync(
            new CreateCongregationRequest("ZZ-Zoekbare Gemeente", "ZZ-Stad", null, null, null, null, null),
            "tester");

        var results = await _congregations.SearchCongregationsAsync("ZZ-Zoekbare");

        Assert.That(results.Any(r => r.Id == created.Id), Is.True,
            "Search must surface a gemeente even when it has no services yet.");
    }

    [Test]
    public async Task UpdateCongregationAsync_SyncsPastorsAndEnforcesSinglePrimary()
    {
        var congregation = await _congregations.CreateCongregationAsync(
            new CreateCongregationRequest("ZZ-TEST Gemeente", "ZZ-Stad", null, null, null, null, null),
            "tester");
        var p1 = await _preachers.CreatePreacherAsync(new CreatePreacherRequest("Ds. Een", null, null), "tester");
        var p2 = await _preachers.CreatePreacherAsync(new CreatePreacherRequest("Ds. Twee", null, null), "tester");

        var updated = await _congregations.UpdateCongregationAsync(
            congregation.Id,
            new UpdateCongregationRequest("ZZ-TEST Gemeente", "ZZ-Stad", null, null, null, null, null,
                new List<CongregationPastorInput>
                {
                    new(p1.Id, true),
                    new(p2.Id, true)
                }),
            "tester");

        Assert.That(updated!.Pastors!.Count, Is.EqualTo(2));
        Assert.That(updated.Pastors!.Count(p => p.IsPrimary), Is.EqualTo(1),
            "Exactly one pastor may be primary.");
    }

    private static CreateServiceRequest BuildServiceRequest(Guid congregationId, Guid? preacherId = null) =>
        new(
            Date: new DateOnly(2026, 1, 4),
            TimeOfDay: (int)TimeOfDay.Morning,
            CongregationId: congregationId,
            PreacherId: preacherId,
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
            Status: null);
}
