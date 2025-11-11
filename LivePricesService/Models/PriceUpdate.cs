namespace LivePricesService.Models
{
    public class PriceUpdate
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
