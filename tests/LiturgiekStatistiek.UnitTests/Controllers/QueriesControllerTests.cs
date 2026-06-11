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

    [SetUp]
    public void SetUp()
    {
        _queryServiceMock = new Mock<IQueryService>();
        _llmServiceMock = new Mock<ILlmService>();
        _sut = new QueriesController(_queryServiceMock.Object, _llmServiceMock.Object);
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
}
