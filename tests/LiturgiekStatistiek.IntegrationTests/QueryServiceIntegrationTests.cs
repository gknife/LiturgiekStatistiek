using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

[TestFixture]
public class QueryServiceIntegrationTests
{
    private ApplicationDbContext _db = null!;
    private QueryService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _db = new ApplicationDbContext(options);
        await DataSeeder.SeedAsync(_db);
        _sut = new QueryService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task MostOpeningSong_ReturnsResults()
    {
        var result = await _sut.ExecuteTemplateAsync("most-opening-song", new Dictionary<string, string>());
        Assert.That(result.Rows, Is.Not.Empty);
        Assert.That(result.ChartType, Is.EqualTo("bar"));
    }

    [Test]
    public async Task AverageSongsPerService_ReturnsDataForAllCongregations()
    {
        var result = await _sut.ExecuteTemplateAsync("average-songs-per-service", new Dictionary<string, string>());
        Assert.That(result.Rows.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task SongUsageOverTime_WithValidSong_ReturnsYearData()
    {
        var bundle = await _db.ListItems.FirstAsync(li => li.Abbreviation == "Ps1773");
        var result = await _sut.ExecuteTemplateAsync("song-usage-over-time", new Dictionary<string, string>
        {
            ["bundleId"] = bundle.Id.ToString(),
            ["songNumber"] = "63"
        });
        Assert.That(result.ChartType, Is.EqualTo("line"));
        Assert.That(result.Rows.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task ServicesWithSong_ReturnsMatchingServices()
    {
        var bundle = await _db.ListItems.FirstAsync(li => li.Abbreviation == "LvdK");
        var result = await _sut.ExecuteTemplateAsync("services-with-song", new Dictionary<string, string>
        {
            ["bundleId"] = bundle.Id.ToString(),
            ["songNumber"] = "91"
        });
        Assert.That(result.Rows.Count, Is.GreaterThan(0));
        Assert.That(result.Columns, Does.Contain("Datum"));
        Assert.That(result.Columns, Does.Contain("Gemeente"));
    }

    [Test]
    public async Task MostSungVerse_Returns2026Data()
    {
        var result = await _sut.ExecuteTemplateAsync("most-sung-verse", new Dictionary<string, string>
        {
            ["year"] = "2026"
        });
        Assert.That(result.Rows.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task SongByPeriod_ReturnsJuneData()
    {
        var result = await _sut.ExecuteTemplateAsync("song-by-period", new Dictionary<string, string>
        {
            ["year"] = "2026",
            ["month"] = "6"
        });
        Assert.That(result.Rows.Count, Is.GreaterThan(0));
    }
}
