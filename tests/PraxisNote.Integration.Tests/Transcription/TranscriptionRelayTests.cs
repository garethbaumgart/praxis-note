using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

namespace PraxisNote.Integration.Tests.Transcription;

public class TranscriptionRelayTests : IAsyncLifetime
{
    private FakeDeepgramServer _fakeDeepgram = null!;
    private TranscriptionWebApplicationFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _fakeDeepgram = new FakeDeepgramServer();
        var baseUrl = await _fakeDeepgram.StartAsync();
        _factory = new TranscriptionWebApplicationFactory { FakeDeepgramBaseUrl = baseUrl };
    }

    public async Task DisposeAsync()
    {
        await _fakeDeepgram.DisposeAsync();
        await _factory.DisposeAsync();
    }

    private WebSocketClient CreateWebSocketClient()
    {
        var client = _factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request =>
        {
            // Add mock auth query param for dev environment
            // The request URI is set separately, but we need to add the query param
        };
        return client;
    }

    private Uri BuildWsUri(string? extraQuery = null)
    {
        var baseUri = _factory.Server.BaseAddress;
        var uriBuilder = new UriBuilder(baseUri)
        {
            Scheme = "ws",
            Path = "/api/transcription/stream",
            Query = "mockAuth=test@example.com|Test+User|00000000-0000-0000-0000-000000000001"
        };

        if (extraQuery != null)
        {
            uriBuilder.Query += $"&{extraQuery}";
        }

        return uriBuilder.Uri;
    }

    private static readonly string CannedResult = JsonSerializer.Serialize(new
    {
        type = "Results",
        is_final = true,
        channel = new
        {
            alternatives = new[]
            {
                new
                {
                    transcript = "Hello world",
                    words = new[]
                    {
                        new { word = "Hello", speaker = 0 },
                        new { word = "world", speaker = 0 }
                    }
                }
            }
        }
    });

    [Fact]
    public async Task AudioRelay_SendBinaryAudio_FakeDeepgramReceivesItAndClientReceivesResult()
    {
        // Arrange
        _fakeDeepgram.EnqueueResult(CannedResult);
        var wsClient = CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(BuildWsUri(), CancellationToken.None);

        // Act: Send a binary audio frame
        var audioData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        await ws.SendAsync(
            new ArraySegment<byte>(audioData),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            CancellationToken.None);

        // Assert: Fake Deepgram received the audio
        await WaitForConditionAsync(() => !_fakeDeepgram.ReceivedAudioFrames.IsEmpty);
        Assert.True(_fakeDeepgram.ReceivedAudioFrames.TryDequeue(out var receivedFrame));
        Assert.Equal(audioData, receivedFrame);

        // Assert: Client receives the canned result
        var resultBuffer = new byte[4096];
        using var resultCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await ws.ReceiveAsync(resultBuffer, resultCts.Token);
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);

        var json = Encoding.UTF8.GetString(resultBuffer, 0, result.Count);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Results", doc.RootElement.GetProperty("type").GetString());

        // Act: Client sends close
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);

        // Assert: Fake Deepgram received CloseStream
        await WaitForConditionAsync(() => !_fakeDeepgram.ReceivedTextMessages.IsEmpty);
        Assert.True(_fakeDeepgram.ReceivedTextMessages.TryDequeue(out var textMsg));
        Assert.Contains("CloseStream", textMsg);
    }

    [Fact]
    public async Task AudioRelay_RapidFrames_NoBufferCorruption()
    {
        // Arrange
        var wsClient = CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(BuildWsUri(), CancellationToken.None);

        const int frameCount = 100;

        // Act: Send 100 frames rapidly, each with a unique byte pattern
        for (var i = 0; i < frameCount; i++)
        {
            var frame = new byte[64];
            Array.Fill(frame, (byte)i);
            await ws.SendAsync(
                new ArraySegment<byte>(frame),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                CancellationToken.None);
        }

        // Close to flush
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);

        // Assert: Fake Deepgram received all frames
        await WaitForConditionAsync(
            () => _fakeDeepgram.ReceivedAudioFrames.Count >= frameCount,
            timeoutMs: 10000);

        Assert.Equal(frameCount, _fakeDeepgram.ReceivedAudioFrames.Count);

        // Assert: Each frame has the correct byte pattern (no cross-contamination)
        var frames = _fakeDeepgram.ReceivedAudioFrames.ToArray();
        for (var i = 0; i < frameCount; i++)
        {
            Assert.Equal(64, frames[i].Length);
            Assert.All(frames[i], b => Assert.Equal((byte)i, b));
        }
    }

    [Fact]
    public async Task ResultsRelay_MultiFrameJson_ClientReceivesCompleteMessage()
    {
        // Arrange: Configure fake server to send a result split across 3 WebSocket frames
        _fakeDeepgram.EnqueueMultiFrameResult(CannedResult, 3);
        var wsClient = CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(BuildWsUri(), CancellationToken.None);

        // Act: Send one audio frame to trigger the result
        var audioData = new byte[] { 0xFF };
        await ws.SendAsync(
            new ArraySegment<byte>(audioData),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            CancellationToken.None);

        // Assert: Client receives a single complete text message
        var resultBuffer = new byte[8192];
        using var resultCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await ws.ReceiveAsync(resultBuffer, resultCts.Token);
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        Assert.True(result.EndOfMessage);

        // Assert: JSON parses correctly and contains the full result
        var json = Encoding.UTF8.GetString(resultBuffer, 0, result.Count);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Results", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("Hello world",
            doc.RootElement
                .GetProperty("channel")
                .GetProperty("alternatives")[0]
                .GetProperty("transcript")
                .GetString());

        // Clean up
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
    }

    [Fact]
    public async Task KeepAlive_NoAudioSent_FakeDeepgramReceivesKeepAliveMessages()
    {
        // Arrange: KeepAliveIntervalSeconds is set to 1 in the WAF
        var wsClient = CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(BuildWsUri(), CancellationToken.None);

        // Act: Send one audio frame, then wait 3+ seconds without sending audio
        var audioData = new byte[] { 0x01 };
        await ws.SendAsync(
            new ArraySegment<byte>(audioData),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            CancellationToken.None);

        // Wait for KeepAlive messages to be sent (interval is 1 second)
        await Task.Delay(3500);

        // Close client
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);

        // Assert: Fake Deepgram received at least 1 KeepAlive message
        var textMessages = _fakeDeepgram.ReceivedTextMessages.ToArray();
        var keepAliveMessages = textMessages.Where(m => m.Contains("KeepAlive")).ToArray();
        Assert.NotEmpty(keepAliveMessages);
        Assert.All(keepAliveMessages, m =>
        {
            using var doc = JsonDocument.Parse(m);
            Assert.Equal("KeepAlive", doc.RootElement.GetProperty("type").GetString());
        });
    }

    [Fact]
    public async Task DeepgramDisconnect_ClientReceivesCloseWithReason()
    {
        // Arrange: Configure fake server to close after 2 audio frames
        _fakeDeepgram.CloseAfterFrames(
            frameCount: 2,
            WebSocketCloseStatus.InternalServerError,
            "API key expired");

        var wsClient = CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(BuildWsUri(), CancellationToken.None);

        // Act: Send 2 audio frames
        for (var i = 0; i < 2; i++)
        {
            await ws.SendAsync(
                new ArraySegment<byte>(new byte[] { (byte)i }),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                CancellationToken.None);
            await Task.Delay(100); // Small delay to ensure processing
        }

        // Assert: Client receives a close event with the error
        var buffer = new byte[4096];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await ws.ReceiveAsync(buffer, cts.Token);
        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(WebSocketCloseStatus.InternalServerError, result.CloseStatus);
        Assert.Contains("API key expired", result.CloseStatusDescription);
    }

    [Fact]
    public async Task GracefulShutdown_ClientCloses_DeepgramReceivesCloseStream()
    {
        // Arrange
        var wsClient = CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(BuildWsUri(), CancellationToken.None);

        // Act: Send one audio frame then close
        var audioData = new byte[] { 0xAB, 0xCD };
        await ws.SendAsync(
            new ArraySegment<byte>(audioData),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            CancellationToken.None);

        await Task.Delay(100); // Ensure frame is processed

        // Client sends close
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Recording stopped", CancellationToken.None);

        // Assert: Fake Deepgram received CloseStream
        await WaitForConditionAsync(
            () => _fakeDeepgram.ReceivedTextMessages.Any(m => m.Contains("CloseStream")));

        var textMessages = _fakeDeepgram.ReceivedTextMessages.ToArray();
        var closeStreamMsg = textMessages.FirstOrDefault(m => m.Contains("CloseStream"));
        Assert.NotNull(closeStreamMsg);

        using var doc = JsonDocument.Parse(closeStreamMsg);
        Assert.Equal("CloseStream", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task UnexpectedTextMessage_ContinuesProcessing()
    {
        // Arrange
        _fakeDeepgram.EnqueueResult(CannedResult);
        var wsClient = CreateWebSocketClient();
        using var ws = await wsClient.ConnectAsync(BuildWsUri(), CancellationToken.None);

        // Act: Send a text message (unexpected — relay expects binary audio)
        var textBytes = Encoding.UTF8.GetBytes("unexpected text");
        await ws.SendAsync(
            new ArraySegment<byte>(textBytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        await Task.Delay(200); // Let the relay process the unexpected message

        // Act: Send a binary audio frame after the text message
        var audioData = new byte[] { 0xDE, 0xAD };
        await ws.SendAsync(
            new ArraySegment<byte>(audioData),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            CancellationToken.None);

        // Assert: Fake Deepgram received the binary frame (text was not forwarded as audio)
        await WaitForConditionAsync(() => !_fakeDeepgram.ReceivedAudioFrames.IsEmpty);
        Assert.True(_fakeDeepgram.ReceivedAudioFrames.TryDequeue(out var receivedFrame));
        Assert.Equal(new byte[] { 0xDE, 0xAD }, receivedFrame);

        // Assert: Client can still receive results (relay didn't crash)
        var resultBuffer = new byte[4096];
        using var resultCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await ws.ReceiveAsync(resultBuffer, resultCts.Token);
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);

        // Clean up
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
    }

    private static async Task WaitForConditionAsync(
        Func<bool> condition, int timeoutMs = 5000, int pollIntervalMs = 50)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(pollIntervalMs);
        }

        if (!condition())
        {
            throw new TimeoutException(
                $"Condition was not met within {timeoutMs}ms");
        }
    }
}
