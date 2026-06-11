using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Infrastructure.Services;
using LiturgiekStatistiek.Domain.Interfaces;
using NUnit.Framework;
using Moq;

namespace LiturgiekStatistiek.UnitTests.Services;

[TestFixture]
public class QueryServiceTests
{
    private QueryService _sut = null!;
    private Mock<IApplicationDbContext> _dbMock = null!;

    [SetUp]
    public void SetUp()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _sut = new QueryService(_dbMock.Object);
    }

    [Test]
    public async Task GetTemplatesAsync_Returns10Templates()
    {
        var templates = await _sut.GetTemplatesAsync();
        Assert.That(templates, Has.Count.EqualTo(10));
    }

    [Test]
    public async Task GetTemplatesAsync_AllTemplatesHaveRequiredFields()
    {
        var templates = await _sut.GetTemplatesAsync();

        foreach (var template in templates)
        {
            Assert.That(template.Id, Is.Not.Null.And.Not.Empty, "Template must have an Id");
            Assert.That(template.Title, Is.Not.Null.And.Not.Empty, "Template must have a Title");
            Assert.That(template.Description, Is.Not.Null.And.Not.Empty, "Template must have a Description");
            Assert.That(template.DefaultChartType, Is.Not.Null.And.Not.Empty, "Template must have a DefaultChartType");
        }
    }

    [Test]
    public async Task GetTemplatesAsync_TemplateIdsAreUnique()
    {
        var templates = await _sut.GetTemplatesAsync();
        var ids = templates.Select(t => t.Id).ToList();
        Assert.That(ids, Is.Unique);
    }

    [TestCase("most-sung-song")]
    [TestCase("most-sung-verse")]
    [TestCase("most-opening-song")]
    [TestCase("average-songs-per-service")]
    [TestCase("most-psalms-congregation")]
    [TestCase("song-by-city-map")]
    [TestCase("song-by-period")]
    [TestCase("services-with-song")]
    [TestCase("song-after-song")]
    [TestCase("song-usage-over-time")]
    public async Task GetTemplatesAsync_ContainsExpectedTemplate(string expectedId)
    {
        var templates = await _sut.GetTemplatesAsync();
        Assert.That(templates.Any(t => t.Id == expectedId), Is.True, $"Missing template: {expectedId}");
    }

    [Test]
    public async Task ExecuteTemplateAsync_UnknownTemplate_ReturnsErrorResult()
    {
        var result = await _sut.ExecuteTemplateAsync("unknown-template", new Dictionary<string, string>());
        Assert.That(result.Title, Is.EqualTo("Onbekende query"));
    }

    [Test]
    public async Task ExecuteNaturalLanguageAsync_ReturnsPlaceholder()
    {
        var result = await _sut.ExecuteNaturalLanguageAsync("Welk lied wordt het meest gezongen?");
        Assert.That(result.Title, Is.EqualTo("Natuurlijke taalverwerking"));
    }
}
