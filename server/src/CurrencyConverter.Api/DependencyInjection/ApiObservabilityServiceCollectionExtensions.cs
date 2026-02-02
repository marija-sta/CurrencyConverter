using System.Diagnostics.CodeAnalysis;
using CurrencyConverter.Api.Middleware;
using CurrencyConverter.Api.Observability;
using CurrencyConverter.Application.Abstractions.Observability;

namespace CurrencyConverter.Api.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class ApiObservabilityServiceCollectionExtensions
{
	public static IServiceCollection AddApiObservability(this IServiceCollection services, IConfiguration configuration)
	{

		services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

		services.AddSingleton<CorrelationIdMiddleware>();
		services.AddSingleton<RequestLogContextMiddleware>();

		return services;
	}
}