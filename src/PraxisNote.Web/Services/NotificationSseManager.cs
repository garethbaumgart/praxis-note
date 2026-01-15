using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace PraxisNote.Web.Services;

/// <summary>
/// Manages SSE connections for real-time notification updates.
/// </summary>
public sealed class NotificationSseManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<HttpResponse>> _connections = new();

    public void AddConnection(Guid userId, HttpResponse response)
    {
        var connections = _connections.GetOrAdd(userId, _ => []);
        connections.Add(response);
    }

    public void RemoveConnection(Guid userId, HttpResponse response)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            // ConcurrentBag doesn't support removal, so we rebuild without the response
            var remaining = connections.Where(r => r != response).ToList();
            _connections[userId] = new ConcurrentBag<HttpResponse>(remaining);
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

        foreach (var response in connections)
        {
            try
            {
                await response.Body.WriteAsync(bytes);
                await response.Body.FlushAsync();
            }
            catch
            {
                // Connection closed, will be cleaned up
            }
        }
    }

    public async Task BroadcastToAllAsync(string eventName, object data)
    {
        var tasks = _connections.Keys.Select(userId => BroadcastToUserAsync(userId, eventName, data));
        await Task.WhenAll(tasks);
    }
}
