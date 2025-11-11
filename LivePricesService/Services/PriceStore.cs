using System.Collections.Concurrent;
using LivePricesService.Models;

namespace LivePricesService.Services
{
    public class PriceStore : IPriceStore
    {
        private readonly ConcurrentDictionary<string, PriceUpdate> _prices = new(StringComparer.OrdinalIgnoreCase);

        public void SetPrice(PriceUpdate update)
        {
            if (update?.Symbol == null) return;
            _prices.AddOrUpdate(update.Symbol, update, (_, __) => update);
        }

        public bool TryGetPrice(string symbol, out PriceUpdate update)
        {
            return _prices.TryGetValue(symbol ?? string.Empty, out update!);
        }
    }
}
