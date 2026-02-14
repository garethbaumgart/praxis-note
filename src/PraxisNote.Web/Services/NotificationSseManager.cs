using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PraxisNote.Web.Services;

/// <summary>
/// Manages SSE connections for real-time notification updates.
/// </summary>
public sealed class NotificationSseManager(ILogger<NotificationSseManager> logger)
{
    // Using ConcurrentDictionary<HttpResponse, byte> as a concurrent set for O(1) removal
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<HttpResponse, byte>> _connections = new();

    public void AddConnection(Guid userId, HttpResponse response)
    {
        var connections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<HttpResponse, byte>());
        connections.TryAdd(response, 0);
    }

    public void RemoveConnection(Guid userId, HttpResponse response)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            connections.TryRemove(response, out _);
        }
    }

    public async Task BroadcastToUserAsync(Guid userId, string eventName, object data)
    {
        if (!_connections.TryGetValue(userId, out var connections))
        {
            return;
        }

        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventName}\ndata: {json}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);

        var deadConnections = new List<HttpResponse>();

        foreach (var response in connections.Keys)
        {
            try
            {
                await response.Body.WriteAsync(bytes);
                await response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "SSE write failed for user {UserId}, removing dead connection", userId);
                deadConnections.Add(response);
            }
        }

        // Clean up dead connections
        foreach (var dead in deadConnections)
        {
            connections.TryRemove(dead, out _);
        }
    }

    public async Task BroadcastToAllAsync(string eventName, object data)
    {
        var tasks = _connections.Keys.Select(userId => BroadcastToUserAsync(userId, eventName, data));
        await Task.WhenAll(tasks);
    }
}
