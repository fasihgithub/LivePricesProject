using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var ws = new ClientWebSocket();

        try
        {
            await ws.ConnectAsync(new Uri("ws://localhost:5132/ws"), CancellationToken.None);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connected to WebSocket server.");

            // Subscribe to BTCUSD
            var subMsg = "{\"action\":\"subscribe\",\"symbols\":[\"BTCUSD\"]}";
            await ws.SendAsync(Encoding.UTF8.GetBytes(subMsg), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Subscribed to BTCUSD.");

            var buffer = new byte[4096];

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Server closed the connection.");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                try
                {
                    var json = JsonDocument.Parse(message);
                    if (json.RootElement.TryGetProperty("symbol", out var symbolProp) &&
                        json.RootElement.TryGetProperty("price", out var priceProp) &&
                        json.RootElement.TryGetProperty("timestamp", out var tsProp))
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Symbol: {symbolProp.GetString()}, Price: {priceProp.GetDecimal()}, ServerTime: {tsProp.GetString()}");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Raw message: {message}");
                    }
                }
                catch (JsonException)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Failed to parse message: {message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
        }
    }
}
