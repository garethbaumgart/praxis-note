using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading;
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

    private static async Task<IResult> HandleStatus(
        IOptions<DeepgramSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("TranscriptionEndpoints");
        var apiKey = settings.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Results.Ok(new
            {
                available = false,
                reason = "Transcription service is not configured.",
            });
        }

        // Test actual connectivity to Deepgram using a lightweight REST endpoint.
        // GET /v1/projects validates the API key and confirms the service is reachable
        // without incurring any transcription billing.
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token, cancellationToken);
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Token", apiKey);

            var baseUrl = settings.Value.BaseUrl;
            var httpScheme = IsLoopbackAddress(baseUrl) ? "http" : "https";
            using var response = await httpClient.GetAsync(
                $"{httpScheme}://{baseUrl}/v1/projects", linkedCts.Token);

            if (response.IsSuccessStatusCode)
            {
                return Results.Ok(new { available = true });
            }

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogWarning("Deepgram API key is invalid (HTTP {StatusCode})",
                    (int)response.StatusCode);
                return Results.Ok(new
                {
                    available = false,
                    reason = "Transcription API key is invalid.",
                });
            }

            logger.LogWarning("Deepgram connectivity check failed (HTTP {StatusCode})",
                (int)response.StatusCode);
            return Results.Ok(new
            {
                available = false,
                reason = "Transcription service returned an error. Please try again.",
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected — let the framework handle it
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Deepgram connectivity check timed out");
            return Results.Ok(new
            {
                available = false,
                reason = "Transcription service is not responding. Please try again.",
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Deepgram connectivity check failed");
            return Results.Ok(new
            {
                available = false,
                reason = "Cannot reach transcription service. Please check your connection.",
            });
        }
    }

    private static async Task HandleStream(
        HttpContext context,
        IOptions<DeepgramSettings> settings,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TranscriptionEndpoints");
        var sessionId = Guid.NewGuid().ToString("N")[..8];

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
            logger.LogWarning("[{SessionId}] Transcription WebSocket rejected: user not authenticated", sessionId);
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
            logger.LogWarning("[{SessionId}] Transcription WebSocket rejected: Deepgram API key not configured", sessionId);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Transcription service not configured.");
            return;
        }

        using var clientWs = await context.WebSockets.AcceptWebSocketAsync();
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        logger.LogInformation("[{SessionId}] Transcription session started for user {UserId}", sessionId, userId);

        // Build Deepgram streaming URL
        var channels = context.Request.Query["channels"].FirstOrDefault();
        var mimeType = context.Request.Query["mimeType"].FirstOrDefault();
        var encoding = context.Request.Query["encoding"].FirstOrDefault();
        var sampleRate = context.Request.Query["sampleRate"].FirstOrDefault();
        var isMultichannel = int.TryParse(channels, out var channelCount) && channelCount > 1;
        var hasExplicitEncoding = !string.IsNullOrEmpty(encoding);

        // Deepgram's multichannel mode requires raw audio with explicit encoding params.
        // Container formats (WebM/Opus, OGG) include their own framing and can only be
        // auto-detected in single-channel mode. When the browser sends a container format
        // (which is always the case with MediaRecorder), we must disable multichannel and
        // fall back to single-channel with diarization for speaker separation.
        // Skip this check when explicit encoding is provided (raw PCM mode).
        if (!hasExplicitEncoding)
        {
            if (isMultichannel && IsContainerFormat(mimeType))
            {
                logger.LogInformation(
                    "[{SessionId}] Multichannel requested with container format (mimeType={MimeType}), " +
                    "falling back to single-channel with diarization",
                    sessionId, mimeType ?? "auto-detect");
                isMultichannel = false;
            }
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

        // Add explicit encoding params to Deepgram URL when provided (raw PCM mode)
        if (hasExplicitEncoding)
        {
            queryParams.Add($"encoding={Uri.EscapeDataString(encoding!)}");
            if (!string.IsNullOrEmpty(sampleRate))
            {
                queryParams.Add($"sample_rate={Uri.EscapeDataString(sampleRate)}");
            }
        }
        else if (!string.IsNullOrEmpty(mimeType))
        {
            // Legacy path: pass mimeType-based encoding for non-container formats
            if (!IsContainerFormat(mimeType, treatEmptyAsContainer: false))
            {
                queryParams.Add($"encoding={Uri.EscapeDataString(mimeType)}");
            }
        }

        // Use ws:// for local/test servers, wss:// for production
        var wsScheme = IsLoopbackAddress(deepgramSettings.BaseUrl) ? "ws" : "wss";
        var deepgramUrl = $"{wsScheme}://{deepgramSettings.BaseUrl}/v1/listen?{string.Join("&", queryParams)}";

        using var deepgramWs = new ClientWebSocket();
        deepgramWs.Options.SetRequestHeader("Authorization", $"Token {deepgramSettings.ApiKey}");
        deepgramWs.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

        try
        {
            using var connectTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var connectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                connectTimeoutCts.Token, context.RequestAborted);
            await deepgramWs.ConnectAsync(new Uri(deepgramUrl), connectLinkedCts.Token);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — clean up silently
            logger.LogInformation("[{SessionId}] Client disconnected during Deepgram WebSocket connect", sessionId);
            return;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[{SessionId}] Deepgram WebSocket connection timed out after 10 seconds", sessionId);
            await CloseIfOpenAsync(clientWs, logger, sessionId,
                WebSocketCloseStatus.InternalServerError,
                "Transcription service connection timed out.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{SessionId}] Failed to connect to Deepgram", sessionId);
            await CloseIfOpenAsync(clientWs, logger, sessionId,
                WebSocketCloseStatus.InternalServerError,
                "Failed to connect to transcription service.");
            return;
        }

        logger.LogInformation("[{SessionId}] Connected to Deepgram, starting relay tasks", sessionId);

        // Notify the client of the actual transcription mode so it can route
        // results correctly (e.g. when multichannel was requested but the backend
        // fell back to single-channel with diarization for container formats).
        var sessionConfig = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "SessionConfig",
            multichannel = isMultichannel,
            diarize = deepgramSettings.Diarize,
        });
        var configBytes = Encoding.UTF8.GetBytes(sessionConfig);
        await clientWs.SendAsync(
            new ArraySegment<byte>(configBytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            context.RequestAborted);

        logger.LogInformation(
            "[{SessionId}] Sent SessionConfig to client: multichannel={IsMultichannel}, diarize={Diarize}",
            sessionId, isMultichannel, deepgramSettings.Diarize);

        // Use StrongBox<long> with Interlocked for thread-safe last-audio timestamp sharing
        var lastAudioSentTicks = new StrongBox<long>(DateTimeOffset.UtcNow.Ticks);
        using var deepgramSendLock = new SemaphoreSlim(1, 1);
        using var sessionCts = new CancellationTokenSource();
        using var audioCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts.Token, context.RequestAborted);
        using var resultsCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts.Token, context.RequestAborted);

        var relayAudio = RelayAudioAsync(clientWs, deepgramWs, audioCts, logger, lastAudioSentTicks, deepgramSendLock, sessionId);
        var relayResults = RelayResultsAsync(deepgramWs, clientWs, resultsCts, logger, sessionId);
        var keepAlive = SendKeepAliveAsync(deepgramWs, sessionCts, lastAudioSentTicks, deepgramSendLock, logger, sessionId, deepgramSettings.KeepAliveIntervalSeconds);

        var completed = await Task.WhenAny(relayAudio, relayResults);
        if (completed == relayAudio)
            await resultsCts.CancelAsync();
        else
            await audioCts.CancelAsync();

        // Cancel the session to stop the keepalive task
        await sessionCts.CancelAsync();

        // Wait for all tasks to finish gracefully
        await Task.WhenAll(relayAudio, relayResults, keepAlive);

        // Clean up connections
        await CloseIfOpenAsync(deepgramWs, logger, sessionId);
        await CloseIfOpenAsync(clientWs, logger, sessionId);

        logger.LogInformation("[{SessionId}] Transcription session ended for user {UserId}", sessionId, userId);
    }

    /// <summary>
    /// Reads binary audio frames from the browser client and forwards them to Deepgram.
    /// Copies received bytes to a new buffer before sending to avoid buffer reuse corruption.
    /// All sends to Deepgram are serialized via deepgramSendLock to prevent concurrent SendAsync calls.
    /// </summary>
    private static async Task RelayAudioAsync(
        WebSocket clientWs,
        ClientWebSocket deepgramWs,
        CancellationTokenSource ownCts,
        ILogger logger,
        StrongBox<long> lastAudioSentTicks,
        SemaphoreSlim deepgramSendLock,
        string sessionId)
    {
        var buffer = new byte[BufferSize];
        long totalBytes = 0;
        long frameCount = 0;

        try
        {
            while (!ownCts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await clientWs.ReceiveAsync(buffer, ownCts.Token);
                }
                catch (WebSocketException ex)
                {
                    logger.LogWarning("[{SessionId}] Client WebSocket receive error: {Message}", sessionId, ex.Message);
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning("[{SessionId}] Client WebSocket in invalid state: {Message}", sessionId, ex.Message);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("[{SessionId}] Client sent close frame, sending CloseStream to Deepgram", sessionId);
                    // Client stopped recording — signal Deepgram to finalize.
                    try
                    {
                        var closeMessage = Encoding.UTF8.GetBytes("{\"type\":\"CloseStream\"}");
                        await deepgramSendLock.WaitAsync(ownCts.Token);
                        try
                        {
                            await deepgramWs.SendAsync(
                                closeMessage,
                                WebSocketMessageType.Text,
                                endOfMessage: true,
                                CancellationToken.None);
                        }
                        finally
                        {
                            deepgramSendLock.Release();
                        }
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send CloseStream to Deepgram: {Message}", sessionId, ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket not open for CloseStream: {Message}", sessionId, ex.Message);
                    }
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    // Copy buffer before sending to avoid reuse corruption
                    var copy = new byte[result.Count];
                    Buffer.BlockCopy(buffer, 0, copy, 0, result.Count);

                    try
                    {
                        await deepgramSendLock.WaitAsync(ownCts.Token);
                        try
                        {
                            await deepgramWs.SendAsync(
                                new ArraySegment<byte>(copy, 0, result.Count),
                                WebSocketMessageType.Binary,
                                result.EndOfMessage,
                                ownCts.Token);
                        }
                        finally
                        {
                            deepgramSendLock.Release();
                        }

                        totalBytes += result.Count;
                        frameCount++;
                        Interlocked.Exchange(ref lastAudioSentTicks.Value, DateTimeOffset.UtcNow.Ticks);
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send audio to Deepgram: {Message}", sessionId, ex.Message);
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket not open for audio send: {Message}", sessionId, ex.Message);
                        break;
                    }
                }
                else if (result.MessageType != WebSocketMessageType.Binary)
                {
                    logger.LogWarning("[{SessionId}] Unexpected message type from client: {MessageType}", sessionId, result.MessageType);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        logger.LogInformation(
            "[{SessionId}] Audio relay stopped. Total bytes: {TotalBytes}, frames: {FrameCount}",
            sessionId, totalBytes, frameCount);
    }

    /// <summary>
    /// Reads JSON transcript results from Deepgram and forwards them to the browser client.
    /// Aggregates multi-frame messages using MemoryStream before sending.
    /// </summary>
    private static async Task RelayResultsAsync(
        ClientWebSocket deepgramWs,
        WebSocket clientWs,
        CancellationTokenSource ownCts,
        ILogger logger,
        string sessionId)
    {
        var buffer = new byte[BufferSize];
        long messageCount = 0;

        try
        {
            while (!ownCts.Token.IsCancellationRequested)
            {
                // Fix 2d: Aggregate multi-frame messages
                using var messageStream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    try
                    {
                        result = await deepgramWs.ReceiveAsync(buffer, ownCts.Token);
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket receive error: {Message}", sessionId, ex.Message);
                        return;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket in invalid state: {Message}", sessionId, ex.Message);
                        return;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.LogWarning(
                            "[{SessionId}] Deepgram closed connection: {CloseStatus} - {CloseDescription}",
                            sessionId, deepgramWs.CloseStatus, deepgramWs.CloseStatusDescription);

                        // Forward the close reason to the browser client so the frontend
                        // can surface a meaningful error instead of silently retrying.
                        // Use CloseOutputAsync with a timeout to avoid hanging if the client
                        // doesn't complete the close handshake.
                        try
                        {
                            var reason = deepgramWs.CloseStatusDescription ?? "Transcription service closed the connection";

                            // RFC 6455: close reason must be <= 123 bytes UTF-8
                            while (Encoding.UTF8.GetByteCount(reason) > 123)
                            {
                                reason = reason[..^4] + "...";
                            }

                            using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await clientWs.CloseOutputAsync(
                                WebSocketCloseStatus.InternalServerError,
                                reason,
                                closeCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            logger.LogWarning("[{SessionId}] Client close handshake timed out, aborting", sessionId);
                            clientWs.Abort();
                        }
                        catch (WebSocketException ex)
                        {
                            logger.LogWarning("[{SessionId}] Failed to forward close to client: {Message}", sessionId, ex.Message);
                        }
                        catch (InvalidOperationException ex)
                        {
                            logger.LogWarning("[{SessionId}] Client WebSocket not open for close forward: {Message}", sessionId, ex.Message);
                        }

                        return;
                    }

                    if (result.Count > 0)
                    {
                        messageStream.Write(buffer, 0, result.Count);
                    }

                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text && messageStream.Length > 0)
                {
                    var messageBytes = messageStream.ToArray();

                    try
                    {
                        await clientWs.SendAsync(
                            new ArraySegment<byte>(messageBytes),
                            WebSocketMessageType.Text,
                            endOfMessage: true,
                            ownCts.Token);

                        messageCount++;
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send result to client: {Message}", sessionId, ex.Message);
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Client WebSocket not open for result send: {Message}", sessionId, ex.Message);
                        break;
                    }
                }
                else if (result.MessageType != WebSocketMessageType.Text)
                {
                    // Fix 2e: Log unexpected message types
                    logger.LogWarning("[{SessionId}] Unexpected message type from Deepgram: {MessageType}", sessionId, result.MessageType);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        logger.LogInformation(
            "[{SessionId}] Results relay stopped. Total messages forwarded: {MessageCount}",
            sessionId, messageCount);
    }

    /// <summary>
    /// Sends periodic KeepAlive messages to Deepgram when no audio has been sent within the interval.
    /// This prevents Deepgram from closing idle connections during recording pauses.
    /// All sends are serialized via deepgramSendLock to prevent concurrent SendAsync calls.
    /// </summary>
    private static async Task SendKeepAliveAsync(
        ClientWebSocket deepgramWs,
        CancellationTokenSource sessionCts,
        StrongBox<long> lastAudioSentTicks,
        SemaphoreSlim deepgramSendLock,
        ILogger logger,
        string sessionId,
        int intervalSeconds)
    {
        var keepAliveMessage = Encoding.UTF8.GetBytes("{\"type\":\"KeepAlive\"}");
        var interval = TimeSpan.FromSeconds(Math.Max(intervalSeconds, 1));

        try
        {
            while (!sessionCts.Token.IsCancellationRequested)
            {
                await Task.Delay(interval, sessionCts.Token);

                var lastSentTicks = Interlocked.Read(ref lastAudioSentTicks.Value);
                var elapsed = DateTimeOffset.UtcNow - new DateTimeOffset(lastSentTicks, TimeSpan.Zero);
                if (elapsed > interval)
                {
                    try
                    {
                        await deepgramSendLock.WaitAsync(sessionCts.Token);
                        try
                        {
                            await deepgramWs.SendAsync(
                                keepAliveMessage,
                                WebSocketMessageType.Text,
                                endOfMessage: true,
                                sessionCts.Token);
                        }
                        finally
                        {
                            deepgramSendLock.Release();
                        }

                        logger.LogDebug("[{SessionId}] Sent KeepAlive to Deepgram", sessionId);
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("[{SessionId}] Failed to send KeepAlive to Deepgram: {Message}", sessionId, ex.Message);
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning("[{SessionId}] Deepgram WebSocket not open for KeepAlive: {Message}", sessionId, ex.Message);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    /// <summary>
    /// Checks if a MIME type corresponds to a container format that Deepgram cannot
    /// process in multichannel mode (WebM, Opus, OGG, MP4).
    /// When treatEmptyAsContainer is true (default), null/empty MIME types are treated
    /// as container formats (safe default for multichannel fallback).
    /// </summary>
    private static bool IsContainerFormat(string? mimeType, bool treatEmptyAsContainer = true)
    {
        if (string.IsNullOrEmpty(mimeType))
            return treatEmptyAsContainer;

        return mimeType.Contains("webm", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("opus", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("ogg", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("mp4", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a base URL (host or host:port) refers to a loopback address.
    /// Matches localhost, 127.x.x.x, and [::1] (with or without port).
    /// </summary>
    private static bool IsLoopbackAddress(string baseUrl)
    {
        return baseUrl.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.StartsWith("127.", StringComparison.Ordinal)
            || baseUrl.StartsWith("[::1]", StringComparison.Ordinal)
            || baseUrl.Equals("::1", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gracefully closes a WebSocket if it's still open. Uses CloseOutputAsync with a 5-second timeout,
    /// falling back to Abort() if the close handshake doesn't complete in time.
    /// </summary>
    private static async Task CloseIfOpenAsync(
        WebSocket ws,
        ILogger logger,
        string sessionId,
        WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure,
        string reason = "Done")
    {
        try
        {
            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ws.CloseOutputAsync(status, reason, timeoutCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Close handshake timed out — force close
            logger.LogWarning("[{SessionId}] WebSocket close timed out, aborting", sessionId);
            ws.Abort();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[{SessionId}] Error closing WebSocket, aborting", sessionId);
            try { ws.Abort(); } catch { /* Best effort */ }
        }
    }
}
