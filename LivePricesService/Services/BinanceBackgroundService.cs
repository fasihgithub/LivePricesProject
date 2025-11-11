using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using LivePricesService.Models;

namespace LivePricesService.Services
{
    public class BinanceBackgroundService : BackgroundService
    {
        private readonly ILogger<BinanceBackgroundService> _logger;
        private readonly WebSocketManager _wsManager;
        private readonly IPriceStore _priceStore;

        private readonly Uri _binanceUri = new("wss://stream.binance.com:443/ws/btcusdt@aggTrade");

        public BinanceBackgroundService(ILogger<BinanceBackgroundService> logger, WebSocketManager wsManager, IPriceStore priceStore)
        {
            _logger = logger;
            _wsManager = wsManager;
            _priceStore = priceStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var ws = new ClientWebSocket();
                    await ws.ConnectAsync(_binanceUri, stoppingToken);
                    _logger.LogInformation("Connected to Binance WebSocket.");

                    var buffer = new byte[16 * 1024];
                    while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                        if (result.MessageType == WebSocketMessageType.Close) break;

                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("p", out var priceProp) && root.TryGetProperty("s", out var symbolProp))
                        {
                            var symbol = symbolProp.GetString();
                            var priceStr = priceProp.GetString();
                            if (!string.IsNullOrEmpty(symbol) && decimal.TryParse(priceStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var price))
                            {
                                var normalizedSymbol = symbol.Replace("USDT", "USD", StringComparison.OrdinalIgnoreCase);
                                var update = new PriceUpdate { Symbol = normalizedSymbol, Price = price, Timestamp = DateTime.UtcNow };
                                _priceStore.SetPrice(update);
                                await _wsManager.BroadcastAsync(update);
                                _logger.LogInformation("Broadcasted {Symbol} {Price}", update.Symbol, update.Price);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in BinanceBackgroundService, retrying in 5s...");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}
