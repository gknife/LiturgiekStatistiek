using LiturgiekStatistiek.Api.Controllers;
using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace LiturgiekStatistiek.UnitTests.Controllers;

[TestFixture]
public class QueriesControllerTests
{
    private QueriesController _sut = null!;
    private Mock<IQueryService> _queryServiceMock = null!;
    private Mock<ILlmService> _llmServiceMock = null!;
    private Mock<IAdvancedQueryService> _advancedQueryServiceMock = null!;
    private Mock<ISavedQueryService> _savedQueryServiceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _queryServiceMock = new Mock<IQueryService>();
        _llmServiceMock = new Mock<ILlmService>();
        _advancedQueryServiceMock = new Mock<IAdvancedQueryService>();
        _savedQueryServiceMock = new Mock<ISavedQueryService>();
        _sut = new QueriesController(
            _queryServiceMock.Object,
            _llmServiceMock.Object,
            _advancedQueryServiceMock.Object,
            _savedQueryServiceMock.Object);
    }

    [Test]
    public async Task GetTemplates_ReturnsOkWithTemplates()
    {
        _queryServiceMock.Setup(s => s.GetTemplatesAsync())
            .ReturnsAsync(new List<QueryTemplate> { new() { Id = "test", Title = "Test" } });

        var result = await _sut.GetTemplates();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var templates = (List<QueryTemplate>)ok.Value!;
        Assert.That(templates, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ExecuteQuery_WithTemplateId_ExecutesTemplate()
    {
        var expected = new QueryResult { Title = "Result", TotalCount = 5 };
        _queryServiceMock.Setup(s => s.ExecuteTemplateAsync("test", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var request = new QueryRequest { TemplateId = "test", Parameters = new Dictionary<string, string>() };
        var result = await _sut.ExecuteQuery(request, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task ExecuteQuery_WithNlQuery_UsesLlmService()
    {
        _llmServiceMock.Setup(s => s.ParseNaturalLanguageQueryAsync("test query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmQueryParseResult
            {
                Success = true,
                TemplateId = "most-sung-song",
                Parameters = new Dictionary<string, string> { ["congregationId"] = "123" }
            });
        _queryServiceMock.Setup(s => s.ExecuteTemplateAsync("most-sung-song", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult { Title = "Meest gezongen" });

        var request = new QueryRequest { NaturalLanguageQuery = "test query" };
        var result = await _sut.ExecuteQuery(request, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        _llmServiceMock.Verify(s => s.ParseNaturalLanguageQueryAsync("test query", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExecuteQuery_WithNlQuery_LlmFails_ReturnsErrorResult()
    {
        _llmServiceMock.Setup(s => s.ParseNaturalLanguageQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmQueryParseResult { Success = false, ErrorMessage = "Niet geconfigureerd" });

        var request = new QueryRequest { NaturalLanguageQuery = "test" };
        var result = await _sut.ExecuteQuery(request, CancellationToken.None);

        var ok = (OkObjectResult)result.Result!;
        var queryResult = (QueryResult)ok.Value!;
        Assert.That(queryResult.Title, Is.EqualTo("Kon vraag niet verwerken"));
    }

    [Test]
    public async Task ExecuteQuery_NoTemplateOrQuery_ReturnsBadRequest()
    {
        var request = new QueryRequest();
        var result = await _sut.ExecuteQuery(request, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetAdvancedSchema_ReturnsOkWithSchema()
    {
        _advancedQueryServiceMock.Setup(s => s.GetSchema())
            .Returns(new AdvancedQuerySchema { Fields = new() { new() { Key = "date" } } });

        var result = _sut.GetAdvancedSchema();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var schema = (AdvancedQuerySchema)ok.Value!;
        Assert.That(schema.Fields, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ExecuteAdvanced_ReturnsOkWithResult()
    {
        _advancedQueryServiceMock.Setup(s => s.ExecuteAsync(It.IsAny<AdvancedQueryDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult { Title = "Geavanceerd", TotalCount = 3 });

        var result = await _sut.ExecuteAdvanced(new AdvancedQueryDefinition(), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var queryResult = (QueryResult)ok.Value!;
        Assert.That(queryResult.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public async Task CompareAdvanced_ReturnsOkWithResult()
    {
        _advancedQueryServiceMock.Setup(s => s.CompareAsync(It.IsAny<IReadOnlyList<AdvancedQueryDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult { Title = "Vergelijking" });

        var request = new CompareQueriesRequest { Queries = new() { new(), new() } };
        var result = await _sut.CompareAdvanced(request, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void GetAiStatus_ReturnsOkWithStatus()
    {
        _llmServiceMock.Setup(s => s.GetStatus())
            .Returns(new LlmStatus { IsConfigured = false, Message = "Niet geconfigureerd" });

        var result = _sut.GetAiStatus();

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var status = (LlmStatus)ok.Value!;
        Assert.That(status.IsConfigured, Is.False);
    }
}
