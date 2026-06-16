using LiturgiekStatistiek.Application.DTOs;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

[TestFixture]
public class ServiceServiceBulkIntegrationTests
{
    private ApplicationDbContext _db = null!;
    private ServiceService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"BulkTestDb_{Guid.NewGuid()}")
            .Options;

        _db = new ApplicationDbContext(options);
        await DataSeeder.SeedAsync(_db);
        _sut = new ServiceService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task BulkUpdateAsync_ChangesTimeOfDayForSelected()
    {
        var ids = await _db.Services.Take(2).Select(s => s.Id).ToListAsync();
        var request = new BulkUpdateServicesRequest(ids, "timeOfDay", ((int)TimeOfDay.Evening).ToString());

        var result = await _sut.BulkUpdateAsync(request, "tester");

        Assert.That(result.Affected, Is.EqualTo(2));
        var updated = await _db.Services.Where(s => ids.Contains(s.Id)).ToListAsync();
        Assert.That(updated.All(s => s.TimeOfDay == TimeOfDay.Evening), Is.True);
    }

    [Test]
    public async Task BulkUpdateAsync_EmptyIds_AffectsNothing()
    {
        var request = new BulkUpdateServicesRequest(new List<Guid>(), "timeOfDay", "2");
        var result = await _sut.BulkUpdateAsync(request, "tester");
        Assert.That(result.Affected, Is.EqualTo(0));
    }

    [Test]
    public void BulkUpdateAsync_UnknownField_Throws()
    {
        var id = _db.Services.First().Id;
        var request = new BulkUpdateServicesRequest(new List<Guid> { id }, "notARealField", "x");
        Assert.ThrowsAsync<ArgumentException>(() => _sut.BulkUpdateAsync(request, "tester"));
    }

    [Test]
    public async Task BulkDeleteAsync_RemovesSelectedServices()
    {
        var ids = await _db.Services.Take(2).Select(s => s.Id).ToListAsync();
        var before = await _db.Services.CountAsync();

        var result = await _sut.BulkDeleteAsync(new BulkDeleteServicesRequest(ids));

        Assert.That(result.Affected, Is.EqualTo(2));
        var after = await _db.Services.CountAsync();
        Assert.That(after, Is.EqualTo(before - 2));
    }

    [Test]
    public async Task BulkDeleteAsync_EmptyIds_AffectsNothing()
    {
        var result = await _sut.BulkDeleteAsync(new BulkDeleteServicesRequest(new List<Guid>()));
        Assert.That(result.Affected, Is.EqualTo(0));
    }
}
