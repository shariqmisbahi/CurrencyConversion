using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CurrencyConversion.Providers;
using CurrencyConversion.Services;
using CurrencyConversion.Models;
using Microsoft.Extensions.Logging;

namespace CurrencyConversion.Providers
{
    public class FrankfurterProvider : IExchangeRateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FrankfurterProvider> _logger;
        private readonly ICacheService _cacheService;
        private readonly HashSet<string> _unsupportedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

        public FrankfurterProvider(
            HttpClient httpClient,
            ILogger<FrankfurterProvider> logger,
            ICacheService cacheService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService;
        }

        async Task<ExchangeRateResponse> IExchangeRateProvider.GetLatestRatesAsync(string baseCurrency)
        {
            ValidateCurrency(baseCurrency);

            var cacheKey = $"latest_{baseCurrency}";

            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                _logger.LogInformation("Fetching latest rates for base currency: {BaseCurrency}", baseCurrency);

                var response = await _httpClient.GetAsync($"latest?from={baseCurrency}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
                _logger.LogDebug("Successfully fetched latest rates for {BaseCurrency}", baseCurrency);

                return result;
            }, TimeSpan.FromMinutes(15));
        }

        async Task<ExchangeRateResponse> IExchangeRateProvider.GetHistoricalRatesAsync(string baseCurrency, DateTime? startDate, DateTime? endDate)
        {
            ValidateCurrency(baseCurrency);

            if (startDate > endDate)
            {
                throw new ArgumentException("Start date cannot be after end date");
            }

            var cacheKey = $"historical_{baseCurrency}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";

#pragma warning disable CS8603 // Possible null reference return.
            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                _logger.LogInformation(
                    "Fetching historical rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate, endDate);

                var response = await _httpClient.GetAsync(
                    $"{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?from={baseCurrency}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
                _logger.LogDebug(
                    "Successfully fetched historical rates for {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate, endDate);

                return result;
            }, TimeSpan.FromHours(1));
#pragma warning restore CS8603 // Possible null reference return.
        }

        public async Task<decimal> ConvertAmountAsync(string fromCurrency, string toCurrency, decimal amount)
        {
            ValidateCurrency(fromCurrency);
            ValidateCurrency(toCurrency);

            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));
            }

            var cacheKey = $"conversion_{fromCurrency}_{toCurrency}";

            var rate = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                _logger.LogInformation(
                    "Fetching conversion rate from {FromCurrency} to {ToCurrency}",
                    fromCurrency, toCurrency);

                var response = await _httpClient.GetAsync($"latest?from={fromCurrency}&to={toCurrency}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
                _logger.LogDebug(
                    "Successfully fetched conversion rate from {FromCurrency} to {ToCurrency}: {Rate}",
                    fromCurrency, toCurrency, result?.rates[toCurrency]);

                return result?.rates[toCurrency];
            }, TimeSpan.FromMinutes(15));

            return (decimal)(amount * rate);
        }

        public bool IsSupportedCurrency(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            {
                return false;
            }

            return !_unsupportedCurrencies.Contains(currencyCode.ToUpper());
        }

        private void ValidateCurrency(string currencyCode)
        {
            if (!IsSupportedCurrency(currencyCode))
            {
                throw new ArgumentException($"Currency {currencyCode} is not supported");
            }
        }
    }
}