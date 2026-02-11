using System.Net.WebSockets;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using PraxisNote.Application.Features.Transcription;
using PraxisNote.Web.Extensions;

namespace PraxisNote.Web.Endpoints;

public static class TranscriptionEndpoints
{
    private const int BufferSize = 16384;

    public static void MapTranscriptionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/transcription")
            .RequireAuthorization();

        group.MapGet("/status", HandleStatus);

        // WebSocket endpoint is mapped outside the auth group because
        // JavaScript's WebSocket API cannot send custom headers (needed for mock auth in dev).
        // Cookie-based auth (production) works automatically since cookies are sent with WS connections.
        // In dev, the mock auth toolbar sets a cookie as well, but we also accept query params as fallback.
        routes.Map("/api/transcription/stream", HandleStream);
    }

    private static IResult HandleStatus(IOptions<DeepgramSettings> settings)
    {
        return Results.Ok(new { available = !string.IsNullOrWhiteSpace(settings.Value.ApiKey) });
    }

    private static async Task HandleStream(
        HttpContext context,
        IOptions<DeepgramSettings> settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TranscriptionEndpoints");

        // Authenticate the WebSocket connection.
        // Cookie auth works automatically. For dev mock auth, accept query param
        // since WebSocket API can't send custom headers.
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            var mockAuth = context.Request.Query["mockAuth"].FirstOrDefault();
            if (!string.IsNullOrEmpty(mockAuth) && context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                // Parse mock auth: "email|name|userId"
                var parts = mockAuth.Split('|');
                if (parts.Length >= 3)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, parts[2]),
                        new Claim(ClaimTypes.Email, parts[0]),
                        new Claim(ClaimTypes.Name, parts[1]),
                    };
                    user = new ClaimsPrincipal(new ClaimsIdentity(claims, "MockAuth"));
                }
            }
        }

        if (user.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("Transcription WebSocket rejected: user not authenticated");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var deepgramSettings = settings.Value;
        if (string.IsNullOrWhiteSpace(deepgramSettings.ApiKey))
        {
            logger.LogWarning("Transcription WebSocket rejected: Deepgram API key not configured");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Transcription service not configured.");
            return;
        }

        using var clientWs = await context.WebSockets.AcceptWebSocketAsync();
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        logger.LogInformation("Transcription session started for user {UserId}", userId);

        // Build Deepgram streaming URL
        var channels = context.Request.Query["channels"].FirstOrDefault();
        var encoding = context.Request.Query["encoding"].FirstOrDefault();
        var isMultichannel = int.TryParse(channels, out var channelCount) && channelCount > 1;

        // Deepgram's multichannel mode requires raw audio with explicit encoding params.
        // Container formats (WebM/Opus, OGG) include their own framing and can only be
        // auto-detected in single-channel mode. When the browser sends a container format
        // (which is always the case with MediaRecorder), we must disable multichannel and
        // fall back to single-channel with diarization for speaker separation.
        var isContainerFormat = string.IsNullOrEmpty(encoding)
            || encoding.Contains("webm", StringComparison.OrdinalIgnoreCase)
            || encoding.Contains("opus", StringComparison.OrdinalIgnoreCase)
            || encoding.Contains("ogg", StringComparison.OrdinalIgnoreCase)
            || encoding.Contains("mp4", StringComparison.OrdinalIgnoreCase);

        if (isMultichannel && isContainerFormat)
        {
            logger.LogInformation(
                "Multichannel requested with container format (encoding={Encoding}), " +
                "falling back to single-channel with diarization",
                encoding ?? "auto-detect");
            isMultichannel = false;
        }

        var queryParams = new List<string>
        {
            $"model={Uri.EscapeDataString(deepgramSettings.Model)}",
            $"punctuate={deepgramSettings.Punctuate.ToString().ToLowerInvariant()}",
            $"interim_results={deepgramSettings.InterimResults.ToString().ToLowerInvariant()}",
            $"language={Uri.EscapeDataString(deepgramSettings.Language)}",
            "utterance_end_ms=1000",
            $"diarize={deepgramSettings.Diarize.ToString().ToLowerInvariant()}",
        };

        if (isMultichannel)
        {
            queryParams.Add("multichannel=true");
            queryParams.Add($"channels={channelCount}");
        }

        // Pass explicit encoding to Deepgram when provided and it's a raw format
        if (!string.IsNullOrEmpty(encoding) && !isContainerFormat)
        {
            queryParams.Add($"encoding={Uri.EscapeDataString(encoding)}");
        }

        var deepgramUrl = $"wss://api.deepgram.com/v1/listen?{string.Join("&", queryParams)}";

        using var deepgramWs = new ClientWebSocket();
        deepgramWs.Options.SetRequestHeader("Authorization", $"Token {deepgramSettings.ApiKey}");

        try
        {
            await deepgramWs.ConnectAsync(new Uri(deepgramUrl), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to Deepgram");
            await clientWs.CloseAsync(
                WebSocketCloseStatus.InternalServerError,
                "Failed to connect to transcription service.",
                CancellationToken.None);
            return;
        }

        using var cts = new CancellationTokenSource();

        var relayAudio = RelayAudioAsync(clientWs, deepgramWs, cts, logger);
        var relayResults = RelayResultsAsync(deepgramWs, clientWs, cts, logger);

        // Wait for either direction to complete (typically client closes first)
        await Task.WhenAny(relayAudio, relayResults);
        await cts.CancelAsync();

        // Clean up connections
        await CloseIfOpenAsync(deepgramWs, logger);
        await CloseIfOpenAsync(clientWs, logger);

        logger.LogInformation("Transcription session ended for user {UserId}", userId);
    }

    /// <summary>
    /// Reads binary audio frames from the browser client and forwards them to Deepgram.
    /// </summary>
    private static async Task RelayAudioAsync(
        WebSocket clientWs,
        ClientWebSocket deepgramWs,
        CancellationTokenSource cts,
        ILogger logger)
    {
        var buffer = new byte[BufferSize];

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await clientWs.ReceiveAsync(buffer, cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Client stopped recording — signal Deepgram to finalize.
                    // Use CancellationToken.None since the other relay task may have already cancelled cts.
                    if (deepgramWs.State == WebSocketState.Open)
                    {
                        var closeMessage = System.Text.Encoding.UTF8.GetBytes("{\"type\":\"CloseStream\"}");
                        await deepgramWs.SendAsync(
                            closeMessage,
                            WebSocketMessageType.Text,
                            endOfMessage: true,
                            CancellationToken.None);
                    }
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary
                    && result.Count > 0
                    && deepgramWs.State == WebSocketState.Open)
                {
                    await deepgramWs.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        WebSocketMessageType.Binary,
                        result.EndOfMessage,
                        cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            logger.LogWarning("Client WebSocket closed prematurely");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error relaying audio to Deepgram");
        }
    }

    /// <summary>
    /// Reads JSON transcript results from Deepgram and forwards them to the browser client.
    /// </summary>
    private static async Task RelayResultsAsync(
        ClientWebSocket deepgramWs,
        WebSocket clientWs,
        CancellationTokenSource cts,
        ILogger logger)
    {
        var buffer = new byte[BufferSize];

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await deepgramWs.ReceiveAsync(buffer, cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogWarning(
                        "Deepgram closed connection: {CloseStatus} - {CloseDescription}",
                        deepgramWs.CloseStatus,
                        deepgramWs.CloseStatusDescription);

                    // Forward the close reason to the browser client so the frontend
                    // can surface a meaningful error instead of silently retrying.
                    if (clientWs.State == WebSocketState.Open)
                    {
                        var reason = deepgramWs.CloseStatusDescription ?? "Transcription service closed the connection";
                        var closeCode = deepgramWs.CloseStatus == WebSocketCloseStatus.NormalClosure
                            ? WebSocketCloseStatus.NormalClosure
                            : WebSocketCloseStatus.InternalServerError;
                        await clientWs.CloseAsync(closeCode, reason, CancellationToken.None);
                    }

                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text
                    && result.Count > 0
                    && clientWs.State == WebSocketState.Open)
                {
                    await clientWs.SendAsync(
                        new ArraySegment<byte>(buffer, 0, result.Count),
                        WebSocketMessageType.Text,
                        result.EndOfMessage,
                        cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            logger.LogWarning("Deepgram WebSocket closed prematurely");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error relaying results from Deepgram");
        }
    }

    private static async Task CloseIfOpenAsync(WebSocket ws, ILogger logger)
    {
        try
        {
            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Done",
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error closing WebSocket");
        }
    }
}
