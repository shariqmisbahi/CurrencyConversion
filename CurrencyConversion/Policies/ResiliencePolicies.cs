using Polly;
using Polly.Extensions.Http;

namespace CurrencyConversion.Policies
{
    public static class ResiliencePolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, delay, retryCount, context) =>
                    {
                        logger.LogWarning(
                            "Retry {RetryCount} of {PolicyKey} due to {Exception}",
                            retryCount, context.PolicyKey, exception.Exception?.Message ?? exception.Result.StatusCode.ToString());
                    });
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, breakDelay) =>
                    {
                        logger.LogWarning("Circuit breaker opened for {BreakDelay} due to {Exception}",
                            breakDelay, exception.ToString());
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("Circuit breaker reset");
                    });
        }
    }
}