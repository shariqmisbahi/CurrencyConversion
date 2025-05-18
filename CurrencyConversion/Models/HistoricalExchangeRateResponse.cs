using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CurrencyConversion.Models
{
    public class HistoricalExchangeRateResponse
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; } = 1.0m;

        [JsonPropertyName("base")]
        public string BaseCurrency { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<DateTime, Dictionary<string, decimal>> RatesByDate { get; set; }
            = new Dictionary<DateTime, Dictionary<string, decimal>>();
    }
}