using CurrencyConverter.Application.Abstractions.Caching;
using CurrencyConverter.Application.Abstractions.Providers;
using CurrencyConverter.Infrastructure.Caching;
using CurrencyConverter.Infrastructure.Helpers;
using CurrencyConverter.Infrastructure.Http;
using CurrencyConverter.Infrastructure.Options;
using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace CurrencyConverter.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<ProviderOptions>(configuration.GetSection(ProviderOptions.SectionName));
		services.Configure<FrankfurterOptions>(configuration.GetSection(FrankfurterOptions.SectionName));
		services.Configure<FrankfurterResilienceOptions>(configuration.GetSection(FrankfurterResilienceOptions.SectionName));

		services.AddMemoryCache();
		services.AddSingleton<IExchangeRateCache, MemoryExchangeRateCache>();
		services.AddTransient<OutboundHttpCorrelationLoggingHandler>();

		var resilienceOptions =
			configuration.GetSection(FrankfurterResilienceOptions.SectionName)
						.Get<FrankfurterResilienceOptions>()
			?? new FrankfurterResilienceOptions();

		services.AddHttpClient<FrankfurterCurrencyProvider>((sp, client) =>
				{
					var options = sp.GetRequiredService<IOptions<FrankfurterOptions>>()
									.Value;
					client.BaseAddress = new Uri(options.BaseUrl);
				})
				.AddHttpMessageHandler<OutboundHttpCorrelationLoggingHandler>()
				.AddResilienceHandler("Frankfurter", builder =>
				{
					builder.AddTimeout(TimeSpan.FromSeconds(resilienceOptions.TimeoutSeconds));

					builder.AddRetry(new HttpRetryStrategyOptions
					{
						MaxRetryAttempts = resilienceOptions.RetryMaxAttempts,
						BackoffType = DelayBackoffType.Exponential,
						UseJitter = true,
						Delay = TimeSpan.FromMilliseconds(resilienceOptions.RetryBaseDelayMilliseconds),
						ShouldHandle = static args =>
							ValueTask.FromResult(FrankfurterTransientErrorClassifier.IsTransient(args.Outcome))
					});

					builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
					{
						SamplingDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerSamplingSeconds),
						FailureRatio = resilienceOptions.CircuitBreakerFailureRatio,
						MinimumThroughput = resilienceOptions.CircuitBreakerMinimumThroughput,
						BreakDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerBreakSeconds),
						ShouldHandle = static args =>
							ValueTask.FromResult(FrankfurterTransientErrorClassifier.IsTransient(args.Outcome))
					});
				});

		services.AddKeyedTransient<ICurrencyProvider>(CurrencyProviderKey.Frankfurter, (sp, _) =>
			sp.GetRequiredService<FrankfurterCurrencyProvider>());

		services.AddSingleton<ICurrencyProviderFactory, CurrencyProviderFactory>();

		return services;
	}
}