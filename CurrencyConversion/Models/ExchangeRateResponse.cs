namespace CurrencyConversion.Models
{
    public class ExchangeRateResponse
    {
        public decimal amount { get; set; }
        public string baseCurrency { get; set; }
        public DateTime date { get; set; }
        public Dictionary<string, decimal> rates { get; set; } = new();
    }
}
