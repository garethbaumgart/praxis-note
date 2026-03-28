using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PraxisNote.Application.Features.UserAiKeys;
using PraxisNote.Web.Endpoints;

namespace PraxisNote.Integration.Tests.Endpoints;

public class AiKeyErrorResultsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    #region NoAiKeyResult

    [Fact]
    public async Task NoAiKeyResult_Returns422WithExpectedBody()
    {
        var result = AiKeyErrorResults.NoAiKeyResult();
        var body = await ExecuteAndReadAsync(result);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, body.StatusCode);
        Assert.Equal("no_ai_key", body.Doc.GetProperty("error").GetString());
        Assert.Equal("/settings/ai-keys", body.Doc.GetProperty("settingsUrl").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.Doc.GetProperty("message").GetString()));
    }

    #endregion

    #region AiKeyInvalidResult

    [Fact]
    public async Task AiKeyInvalidResult_Returns422WithProviderInMessage()
    {
        var ex = new AiKeyInvalidException("Anthropic");
        var result = AiKeyErrorResults.AiKeyInvalidResult(ex);
        var body = await ExecuteAndReadAsync(result);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, body.StatusCode);
        Assert.Equal("ai_key_invalid", body.Doc.GetProperty("error").GetString());
        Assert.Equal("/settings/ai-keys", body.Doc.GetProperty("settingsUrl").GetString());
        Assert.Contains("Anthropic", body.Doc.GetProperty("message").GetString());
    }

    #endregion

    #region AiRateLimitedResult

    [Fact]
    public async Task AiRateLimitedResult_Returns422WithRetryAfterSeconds()
    {
        var ex = new AiRateLimitedException("Gemini", 30);
        var result = AiKeyErrorResults.AiRateLimitedResult(ex);
        var body = await ExecuteAndReadAsync(result);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, body.StatusCode);
        Assert.Equal("ai_rate_limited", body.Doc.GetProperty("error").GetString());
        Assert.Equal(30, body.Doc.GetProperty("retryAfterSeconds").GetInt32());
        Assert.Contains("Gemini", body.Doc.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AiRateLimitedResult_RetryAfterNull_ReturnsNullInBody()
    {
        var ex = new AiRateLimitedException("OpenAI");
        var result = AiKeyErrorResults.AiRateLimitedResult(ex);
        var body = await ExecuteAndReadAsync(result);

        Assert.Equal("ai_rate_limited", body.Doc.GetProperty("error").GetString());
        Assert.Equal(JsonValueKind.Null, body.Doc.GetProperty("retryAfterSeconds").ValueKind);
    }

    #endregion

    #region AiProviderErrorResult

    [Fact]
    public async Task AiProviderErrorResult_Returns422WithExceptionMessage()
    {
        var ex = new AiProviderException("OpenAI", "OpenAI returned an error. Try again shortly.");
        var result = AiKeyErrorResults.AiProviderErrorResult(ex);
        var body = await ExecuteAndReadAsync(result);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, body.StatusCode);
        Assert.Equal("ai_provider_error", body.Doc.GetProperty("error").GetString());
        Assert.Equal("OpenAI returned an error. Try again shortly.", body.Doc.GetProperty("message").GetString());
    }

    #endregion

    private static async Task<(int StatusCode, JsonElement Doc)> ExecuteAndReadAsync(IResult result)
    {
        var builder = WebApplication.CreateSlimBuilder();
        await using var app = builder.Build();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = app.Services,
        };
        httpContext.Response.Body = new MemoryStream();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        var doc = await JsonSerializer.DeserializeAsync<JsonElement>(httpContext.Response.Body, JsonOptions);
        return (httpContext.Response.StatusCode, doc);
    }
}
