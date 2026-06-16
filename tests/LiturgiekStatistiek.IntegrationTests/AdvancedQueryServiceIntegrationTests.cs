using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace LiturgiekStatistiek.IntegrationTests;

[TestFixture]
public class AdvancedQueryServiceIntegrationTests
{
    private ApplicationDbContext _db = null!;
    private AdvancedQueryService _sut = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"AdvTestDb_{Guid.NewGuid()}")
            .Options;

        _db = new ApplicationDbContext(options);
        await DataSeeder.SeedAsync(_db);
        _sut = new AdvancedQueryService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void GetSchema_ReturnsFieldsAndGroupByOptions()
    {
        var schema = _sut.GetSchema();
        Assert.That(schema.Fields, Is.Not.Empty);
        Assert.That(schema.GroupByFields, Is.Not.Empty);
        Assert.That(schema.Fields.Any(f => f.Key == "songSequence"), Is.True);
        Assert.That(schema.Fields.Any(f => f.Key == "date"), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_NoFilters_ReturnsAllServicesAsList()
    {
        var def = new AdvancedQueryDefinition { OutputMode = "list" };
        var result = await _sut.ExecuteAsync(def);
        Assert.That(result.ChartType, Is.EqualTo("table"));
        Assert.That(result.TotalCount, Is.GreaterThan(0));
        Assert.That(result.Columns, Does.Contain("Datum"));
    }

    [Test]
    public async Task ExecuteAsync_CityFilter_OnlyMatchingCity()
    {
        var def = new AdvancedQueryDefinition
        {
            OutputMode = "list",
            Filters = new() { new() { Field = "city", Operator = "eq", Value = "Zutphen" } }
        };
        var result = await _sut.ExecuteAsync(def);
        Assert.That(result.TotalCount, Is.GreaterThan(0));
        Assert.That(result.Rows.All(r => (string?)r["Plaats"] == "Zutphen"), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_UnknownCity_ReturnsNoRows()
    {
        var def = new AdvancedQueryDefinition
        {
            OutputMode = "list",
            Filters = new() { new() { Field = "city", Operator = "eq", Value = "Nergenshuizen" } }
        };
        var result = await _sut.ExecuteAsync(def);
        Assert.That(result.TotalCount, Is.EqualTo(0));
    }

    [Test]
    public async Task ExecuteAsync_AggregateByCongregation_ReturnsGroups()
    {
        var def = new AdvancedQueryDefinition { OutputMode = "aggregate", GroupBy = "congregation" };
        var result = await _sut.ExecuteAsync(def);
        Assert.That(result.Rows, Is.Not.Empty);
        Assert.That(result.Columns, Does.Contain("Groep"));
        Assert.That(result.Columns, Does.Contain("Aantal"));
        Assert.That(result.Chart, Is.Not.Null);
    }

    [Test]
    public async Task ExecuteAsync_DateBetween_FiltersByRange()
    {
        var def = new AdvancedQueryDefinition
        {
            OutputMode = "list",
            Filters = new()
            {
                new() { Field = "date", Operator = "between", Value = "2026-06-01", Value2 = "2026-06-30" }
            }
        };
        var result = await _sut.ExecuteAsync(def);
        Assert.That(result.TotalCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task CompareAsync_TwoQueries_ReturnsComparisonResult()
    {
        var defs = new List<AdvancedQueryDefinition>
        {
            new() { Name = "Zutphen", OutputMode = "list",
                Filters = new() { new() { Field = "city", Operator = "eq", Value = "Zutphen" } } },
            new() { Name = "Apeldoorn", OutputMode = "list",
                Filters = new() { new() { Field = "city", Operator = "eq", Value = "Apeldoorn" } } }
        };
        var result = await _sut.CompareAsync(defs);
        Assert.That(result.Rows, Has.Count.EqualTo(2));
        Assert.That(result.Columns, Does.Contain("Query"));
        Assert.That(result.Chart, Is.Not.Null);
    }

    [Test]
    public async Task CompareAsync_SharedGroupBy_AlignsDatasets()
    {
        var defs = new List<AdvancedQueryDefinition>
        {
            new() { Name = "A", OutputMode = "aggregate", GroupBy = "congregation" },
            new() { Name = "B", OutputMode = "aggregate", GroupBy = "congregation" }
        };
        var result = await _sut.CompareAsync(defs);
        Assert.That(result.Chart, Is.Not.Null);
        Assert.That(result.Chart!.Datasets, Has.Count.EqualTo(2));
        Assert.That(result.Columns, Does.Contain("Groep"));
    }

    [Test]
    public async Task CompareAsync_Empty_ReturnsPlaceholder()
    {
        var result = await _sut.CompareAsync(new List<AdvancedQueryDefinition>());
        Assert.That(result.Rows, Is.Empty);
    }
}
