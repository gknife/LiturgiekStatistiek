using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LiturgiekStatistiek.UnitTests.Services;

[TestFixture]
public class LlmServiceTests
{
    private LlmService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "",
                ["AzureOpenAI:ApiKey"] = "",
            })
            .Build();
        var logger = Mock.Of<ILogger<LlmService>>();
        _sut = new LlmService(config, logger);
    }

    [Test]
    public async Task ParseNaturalLanguageQueryAsync_NotConfigured_ReturnsError()
    {
        var result = await _sut.ParseNaturalLanguageQueryAsync("Welk lied is het populairst?");
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("niet geconfigureerd"));
    }

    [Test]
    public async Task ParseLiturgyTextAsync_NotConfigured_ReturnsError()
    {
        var result = await _sut.ParseLiturgyTextAsync("Voorzang: Ps. 63:2");
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("niet geconfigureerd"));
    }
}
