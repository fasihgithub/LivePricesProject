using Microsoft.AspNetCore.Mvc;
using LivePricesService.Models;
using LivePricesService.Services;

namespace LivePricesService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InstrumentsController : ControllerBase
    {
        private static readonly List<Instrument> Instruments = new()
        {
            new Instrument { Symbol = "EURUSD", Name = "Euro / US Dollar" },
            new Instrument { Symbol = "USDJPY", Name = "US Dollar / Japanese Yen" },
            new Instrument { Symbol = "BTCUSD", Name = "Bitcoin / US Dollar" }
        };

        private readonly IPriceStore _priceStore;

        public InstrumentsController(IPriceStore priceStore)
        {
            _priceStore = priceStore;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Instrument>> GetInstruments() => Instruments;

        [HttpGet("{symbol}/price")]
        public ActionResult<PriceUpdate> GetPrice(string symbol)
        {
            if (_priceStore.TryGetPrice(symbol, out var update))
            {
                return Ok(update);
            }

            // fallback: return 404 if no live price
            return NotFound(new { message = $"Price for {symbol} not available yet." });
        }
    }
}
