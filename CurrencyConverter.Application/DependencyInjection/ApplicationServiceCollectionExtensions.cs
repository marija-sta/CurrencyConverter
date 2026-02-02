using System.Diagnostics.CodeAnalysis;
using CurrencyConverter.Application.Abstractions.Services;
using CurrencyConverter.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Application.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class ApplicationServiceCollectionExtensions
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		services.AddScoped<IExchangeRatesService, ExchangeRatesService>();
		services.AddScoped<IConversionService, ConversionService>();
		return services;
	}
}