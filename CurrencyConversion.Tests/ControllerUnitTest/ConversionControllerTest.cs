using System;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;
using CurrencyConversion;
using CurrencyConversion.Tests;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Net.Http;

namespace CurrencyConversion.Tests.ControllerUnitTest
{
    public class CurrencyConversionControllerTest
    {
        //private readonly Mock<ICategoryService> _catMock;
        //private readonly TestData _testData = new TestData();
        private CurrencyController _catController;
        private readonly Mock<IMemoryCache> _cacheMock = new();
        private readonly Mock<IHttpClientFactory> _httpFactoryMock = new();
        private readonly Mock<AsyncRetryPolicy<HttpResponseMessage>> _retryMock = new();
        private readonly MockHttpMessageHandler _mockHttp = new();
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new();
        private readonly Dictionary<object, object> _cacheStore = new();

        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;
        //private CurrencyController BuildController() { /* code above */ }


        public CurrencyConversionControllerTest()
        {
            var client = _mockHttp.ToHttpClient();
            _httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            // IMemoryCache.TryGetValue setup
            _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                      .Returns((object k, out object v) =>
                      {
                          _cacheStore.TryGetValue(k, out v);   // assign the out param
                          return _cacheStore.ContainsKey(k);   // return true/false
                      });

            // IMemoryCache.Set setup
            _cacheMock.Setup(c => c.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                      .Callback<object, object, TimeSpan>((k, v, _) => _cacheStore[k] = v);

            // Retry policy just executes the delegate
            _retryMock.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<HttpResponseMessage>>>()))
                      .Returns<Func<Task<HttpResponseMessage>>>(f => f());

            // Removed the incorrect `return` statement and replaced it with proper initialization
            _catController = new CurrencyController(
                _cacheMock.Object,
                _httpFactoryMock.Object,
                _retryMock.Object
            );
        }


        public async Task Returns_Cached_Rates_When_Present()
        {
            // arrange
            var cached = new Dictionary<string, decimal> { ["USD"] = 1m };
            _cacheStore["EUR"] = cached;

            // act
            var result = await _catController.GetLatestExchangeRates("EUR");

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(cached, ok.Value);
        }


        /* ---------- HELPERS ---------- */
        private delegate void TryGetValueCallback(object key, out object value);
    }

    /* Dummy controller & DTO just for compilation */
    public class ExchangeRateResponse
    {
        public Dictionary<string, decimal> rates { get; set; }
    }

    public class CurrencyController : ControllerBase
    {
        public const string BaseUrl = "https://api.frankfurter.app";

        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpFactory;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public CurrencyController(IMemoryCache cache,
                                  IHttpClientFactory httpFactory,
                                  AsyncRetryPolicy<HttpResponseMessage> retryPolicy)
        {
            _cache = cache;
            _httpFactory = httpFactory;
            _retryPolicy = retryPolicy;
        }

        [HttpGet("latest/{baseCurrency}")]
        public async Task<IActionResult> GetLatestExchangeRates(string baseCurrency)
        {
            if (_cache.TryGetValue(baseCurrency, out Dictionary<string, decimal> cachedRates))
                return Ok(cachedRates);

            var client = _httpFactory.CreateClient();
            var response = await _retryPolicy.ExecuteAsync(() =>
                client.GetAsync($"{BaseUrl}/latest?from={baseCurrency}"));

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Failed to fetch exchange rates.");

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ExchangeRateResponse>(content);

            if (data == null)
                return BadRequest("Invalid response from provider");

            _cache.Set(baseCurrency, data.rates, TimeSpan.FromMinutes(10));
            return Ok(data.rates);
        }
    }
}