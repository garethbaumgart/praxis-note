using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PraxisNote.Integration.Tests.Transcription;

/// <summary>
/// In-process WebSocket server that mimics Deepgram's streaming API.
/// Listens on a random port and records all received audio frames.
/// Can be configured to send canned JSON results or simulate errors.
/// </summary>
public sealed class FakeDeepgramServer : IAsyncDisposable
{
    private WebApplication? _app;
    private string _baseUrl = "";

    /// <summary>
    /// Recorded binary audio frames received from the relay.
    /// </summary>
    public ConcurrentQueue<byte[]> ReceivedAudioFrames { get; } = new();

    /// <summary>
    /// Recorded text messages received from the relay (KeepAlive, CloseStream, etc.).
    /// </summary>
    public ConcurrentQueue<string> ReceivedTextMessages { get; } = new();

    /// <summary>
    /// Queue of JSON results to send back to the relay when audio is received.
    /// </summary>
    private readonly ConcurrentQueue<(string Json, int FrameCount)> _pendingResults = new();

    /// <summary>
    /// If set, close the WebSocket after this many audio frames.
    /// </summary>
    private int _closeAfterFrames;
    private WebSocketCloseStatus _closeStatus;
    private string _closeReason = "";

    /// <summary>
    /// Queue a JSON result to send to the relay when next audio is received.
    /// Sent as a single WebSocket frame.
    /// </summary>
    public void EnqueueResult(string json)
    {
        _pendingResults.Enqueue((json, 1));
    }

    /// <summary>
    /// Queue a JSON result to be split across multiple WebSocket frames.
    /// </summary>
    public void EnqueueMultiFrameResult(string json, int frameCount)
    {
        _pendingResults.Enqueue((json, frameCount));
    }

    /// <summary>
    /// Configure the server to close the WebSocket after receiving N audio frames.
    /// </summary>
    public void CloseAfterFrames(int frameCount, WebSocketCloseStatus status, string reason)
    {
        _closeAfterFrames = frameCount;
        _closeStatus = status;
        _closeReason = reason;
    }

    /// <summary>
    /// Start the fake server on a random port. Returns "localhost:{port}".
    /// </summary>
    public async Task<string> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        _app = builder.Build();
        _app.UseWebSockets();

        _app.Map("/v1/listen", async (HttpContext context) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await HandleConnectionAsync(ws);
        });

        // Also handle the REST status endpoint used by HandleStatus
        _app.MapGet("/v1/projects", () => Results.Ok(new { projects = Array.Empty<object>() }));

        await _app.StartAsync();

        var address = _app.Urls.First();
        var uri = new Uri(address);
        _baseUrl = $"127.0.0.1:{uri.Port}";
        return _baseUrl;
    }

    private async Task HandleConnectionAsync(WebSocket ws)
    {
        var buffer = new byte[16384];
        var audioFrameCount = 0;

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    var copy = new byte[result.Count];
                    Buffer.BlockCopy(buffer, 0, copy, 0, result.Count);
                    ReceivedAudioFrames.Enqueue(copy);
                    audioFrameCount++;

                    // Send queued results when audio is received
                    if (_pendingResults.TryDequeue(out var pending))
                    {
                        await SendResultAsync(ws, pending.Json, pending.FrameCount);
                    }

                    // Check if we should close after N frames
                    if (_closeAfterFrames > 0 && audioFrameCount >= _closeAfterFrames)
                    {
                        await ws.CloseAsync(_closeStatus, _closeReason, CancellationToken.None);
                        break;
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ReceivedTextMessages.Enqueue(text);
                }
            }
        }
        catch (WebSocketException)
        {
            // Connection closed unexpectedly — expected in some tests
        }
    }

    private static async Task SendResultAsync(WebSocket ws, string json, int frameCount)
    {
        var bytes = Encoding.UTF8.GetBytes(json);

        if (frameCount <= 1)
        {
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                CancellationToken.None);
            return;
        }

        // Split into multiple frames
        var chunkSize = bytes.Length / frameCount;
        for (var i = 0; i < frameCount; i++)
        {
            var offset = i * chunkSize;
            var count = (i == frameCount - 1) ? bytes.Length - offset : chunkSize;
            var isLast = i == frameCount - 1;

            await ws.SendAsync(
                new ArraySegment<byte>(bytes, offset, count),
                WebSocketMessageType.Text,
                endOfMessage: isLast,
                CancellationToken.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
