using CurrencyConverter.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Api.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		services.AddScoped<IExchangeRatesService, ExchangeRatesService>();
		services.AddScoped<IConversionService, ConversionService>();
		return services;
	}
}