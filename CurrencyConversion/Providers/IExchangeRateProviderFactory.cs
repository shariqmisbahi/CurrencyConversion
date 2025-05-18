using CurrencyConversion.Providers;

namespace CurrencyConversion.Providers
{
    public interface IExchangeRateProviderFactory
    {
        IExchangeRateProvider GetProvider(string providerName = "Frankfurter");
    }

    public class ExchangeRateProviderFactory : IExchangeRateProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ExchangeRateProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IExchangeRateProvider GetProvider(string providerName = "Frankfurter")
        {
            return providerName switch
            {
                "Frankfurter" => _serviceProvider.GetRequiredService<FrankfurterProvider>(),
                _ => throw new NotSupportedException($"Provider {providerName} is not supported")
            };
        }
    }
}
