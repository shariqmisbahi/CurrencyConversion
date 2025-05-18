using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using CurrencyConversion.Models;
using Microsoft.AspNetCore.Authorization;

namespace CurrencyConversion.Controllers
{
    [ApiController]
    [Route("api/currency")]
    public class CurrencyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;
        private const string BaseUrl = "https://api.frankfurter.app";
        private const string BaseUrl2 = "https://api.frankfurter.dev/v1";
        private static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

        public CurrencyController(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;

            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            _circuitBreakerPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));
        }

        [HttpGet("latest/{baseCurrency}")]
        public async Task<IActionResult> GetLatestExchangeRates(string baseCurrency)
        {
            if (_cache.TryGetValue(baseCurrency, out Dictionary<string, decimal> cachedRates))
            {
                return Ok(cachedRates);
            }

            var client = _httpClientFactory.CreateClient();
            var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync($"{BaseUrl}/latest?from={baseCurrency}"));

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Failed to fetch exchange rates.");

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ExchangeRateResponse>(content);
            //var data = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
            //return data.Rates;
            if (data == null)
                return BadRequest("Invalid response from provider");

            _cache.Set(baseCurrency, data.rates, TimeSpan.FromMinutes(10));
            return Ok(data.rates);
        }

        [HttpGet("convert")] // Example: /api/currency/convert?from=USD&to=EUR&amount=100
        public async Task<IActionResult> ConvertCurrency(string from, string to, decimal amount)
        {
            if (ExcludedCurrencies.Contains(from) || ExcludedCurrencies.Contains(to))
                return BadRequest("Conversion involving TRY, PLN, THB, or MXN is not allowed.");

            var client = _httpClientFactory.CreateClient();
            var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync($"{BaseUrl}/latest?from={from}&to={to}"));

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Failed to fetch exchange rates.");

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ExchangeRateResponse>(content);

            if (data == null || !data.rates.ContainsKey(to))
                return BadRequest("Invalid response from provider");

            var convertedAmount = amount * data.rates[to];
            return Ok(new { From = from, To = to, Amount = amount, ConvertedAmount = convertedAmount });
        }

        [HttpGet("historical")] // Example: /api/currency/historical?from=EUR&start=2020-01-01&end=2020-01-31&page=1&pageSize=10
        public async Task<IActionResult> GetHistoricalRates(string from, string start, string end, int page = 1, int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync($"{BaseUrl2}/{start}..?symbols={from}"));

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Failed to fetch historical exchange rates.");

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<HistoricalExchangeRateResponse>(content);

            if (data == null)
                return BadRequest("Invalid response from provider");

            var pagedData = data.RatesByDate.Skip((page - 1) * pageSize).Take(pageSize);
            return Ok(pagedData);
        }
    }
}