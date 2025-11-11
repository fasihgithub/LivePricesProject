using LivePricesService.Models;

namespace LivePricesService.Services
{
    public interface IPriceStore
    {
        void SetPrice(PriceUpdate update);
        bool TryGetPrice(string symbol, out PriceUpdate update);
    }
}
