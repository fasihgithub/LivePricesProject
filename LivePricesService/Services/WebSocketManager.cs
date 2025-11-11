using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using LivePricesService.Models;
using Microsoft.Extensions.Logging;

namespace LivePricesService.Services
{
    public class WebSocketManager
    {
        // Tracks all connected clients
        private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

        // Thread-safe subscriptions: ClientId -> symbols
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _subscriptions =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger<WebSocketManager> _logger;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _logger = logger;

            // Periodic log of total connected clients
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    _logger.LogInformation("[{Time}] Total connected clients: {Count}", DateTime.Now, ConnectedClientCount);
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            });
        }

        // Expose number of connected clients
        public int ConnectedClientCount => _clients.Count;

        // Add a new WebSocket client
        public async Task AddClientAsync(string clientId, WebSocket socket)
        {
            _clients.TryAdd(clientId, socket);
            _subscriptions.TryAdd(clientId, new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
            _logger.LogInformation("[{Time}] Client connected: {ClientId}. Total clients: {Count}",
                DateTime.Now, clientId, _clients.Count);

            _ = Task.Run(() => ReceiveLoopAsync(clientId, socket));
        }

        // Remove a client and clean up
        public async Task RemoveClientAsync(string clientId)
        {
            if (_clients.TryRemove(clientId, out var socket))
            {
                _subscriptions.TryRemove(clientId, out _);
                try
                {
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing socket for client {ClientId}", clientId);
                }
            }
            _logger.LogInformation("[{Time}] Client removed: {ClientId}. Total clients: {Count}",
                DateTime.Now, clientId, _clients.Count);
        }

        // Broadcast price updates to relevant clients
        public async Task BroadcastAsync(PriceUpdate update)
        {
            if (update == null) return;

            var message = JsonSerializer.Serialize(update);
            var buffer = Encoding.UTF8.GetBytes(message);
            var tasks = new List<Task>();

            foreach (var kv in _clients)
            {
                var clientId = kv.Key;
                var socket = kv.Value;

                if (!_subscriptions.TryGetValue(clientId, out var symbols)) continue;

                if (symbols.ContainsKey(update.Symbol) && socket.State == WebSocketState.Open)
                    tasks.Add(SafeSendAsync(socket, new ArraySegment<byte>(buffer), clientId, update.Symbol));
            }

            _logger.LogInformation("[{Time}] Broadcasting {Symbol} to {Count} clients", 
                DateTime.Now, update.Symbol, tasks.Count);

            try { await Task.WhenAll(tasks); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error broadcasting {Symbol}", update.Symbol);
            }
        }

        // Safely send a message to a client
        private async Task SafeSendAsync(WebSocket socket, ArraySegment<byte> buffer, string clientId, string symbol)
        {
            try
            {
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send {Symbol} to client {ClientId}", symbol, clientId);
            }
        }

        // Receive loop to handle subscriptions/unsubscriptions from client
        private async Task ReceiveLoopAsync(string clientId, WebSocket socket)
        {
            var buffer = new byte[4 * 1024];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.CloseStatus.HasValue) break;

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    try
                    {
                        var doc = JsonDocument.Parse(msg);

                        if (!doc.RootElement.TryGetProperty("action", out var action)) continue;
                        var act = action.GetString()?.ToLowerInvariant();

                        if (act == "subscribe" && doc.RootElement.TryGetProperty("symbols", out var syms) && syms.ValueKind == JsonValueKind.Array)
                        {
                            var set = _subscriptions.GetOrAdd(clientId, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
                            foreach (var s in syms.EnumerateArray())
                            {
                                var symbol = s.GetString();
                                if (!string.IsNullOrEmpty(symbol))
                                    set.TryAdd(symbol, 0);
                            }
                            _logger.LogInformation("[{Time}] Client {ClientId} subscribed to: {Subs}", 
                                DateTime.Now, clientId, string.Join(",", set.Keys));
                        }
                        else if (act == "unsubscribe" && doc.RootElement.TryGetProperty("symbols", out var symsToRemove) && symsToRemove.ValueKind == JsonValueKind.Array)
                        {
                            if (_subscriptions.TryGetValue(clientId, out var set))
                            {
                                foreach (var s in symsToRemove.EnumerateArray())
                                {
                                    var symbol = s.GetString();
                                    if (!string.IsNullOrEmpty(symbol))
                                        set.TryRemove(symbol, out _);
                                }
                                _logger.LogInformation("[{Time}] Client {ClientId} unsubscribed from: {Subs}", 
                                    DateTime.Now, clientId, string.Join(",", symsToRemove.EnumerateArray().Select(x => x.GetString())));
                            }
                        }
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning(jex, "Invalid JSON from client {ClientId}: {Msg}", clientId, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Receive loop failed for client {ClientId}", clientId);
            }
            finally
            {
                await RemoveClientAsync(clientId);
            }
        }
    }
}
