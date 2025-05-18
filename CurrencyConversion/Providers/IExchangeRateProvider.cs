using CurrencyConversion.Controllers;
using CurrencyConversion.Models;

namespace CurrencyConversion.Providers
{

    public interface IExchangeRateProvider
    {
        Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency);
        Task<ExchangeRateResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime? startDate, DateTime? endDate);
        Task<decimal> ConvertAmountAsync(string fromCurrency, string toCurrency, decimal amount);
        bool IsSupportedCurrency(string currencyCode);
    }
}