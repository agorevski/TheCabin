using System.Net;
using System.Text;
using System.Text.Json;
using TheCabin.Core.Models;
using TheCabin.Core.Services;
using Xunit;

namespace TheCabin.Core.Tests.Services;

public class LlmCommandParserServiceTests
{
    [Fact]
    public async Task ParseAsync_SendsApiKeyAsPerRequestBearerHeader()
    {
        var handler = new StubHandler(CompletionJson("""{"verb":"take","object":"brass lantern","confidence":0.99}"""));
        using var httpClient = new HttpClient(handler);
        var parser = new LlmCommandParserService(httpClient, "test-key", new LocalCommandParser());

        var result = await parser.ParseAsync("pick up the lantern", new GameContext());

        Assert.Equal(0.99, result.Confidence);
        Assert.Equal("Bearer test-key", handler.LastRequest!.Headers.Authorization!.ToString());
        Assert.False(httpClient.DefaultRequestHeaders.Contains("Authorization"));
    }

    [Fact]
    public async Task ParseAsync_ModelReturnsVerbOutsideGrammar_FallsBackToLocalParser()
    {
        var handler = new StubHandler(CompletionJson("""{"verb":"delete_save","object":"lantern","confidence":0.99}"""));
        var parser = new LlmCommandParserService(new HttpClient(handler), "test-key", new LocalCommandParser());

        var result = await parser.ParseAsync("take lantern", new GameContext());

        Assert.Equal("take", result.Verb);
        Assert.NotEqual(0.99, result.Confidence);
    }

    [Fact]
    public void Constructor_MalformedApiKey_ThrowsWithoutEchoingKey()
    {
        using var httpClient = new HttpClient();

        var ex = Assert.Throws<ArgumentException>(
            () => new LlmCommandParserService(httpClient, "bad key", new LocalCommandParser()));

        Assert.DoesNotContain("bad key", ex.Message);
    }

    private static string CompletionJson(string content)
    {
        return JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } });
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public StubHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
