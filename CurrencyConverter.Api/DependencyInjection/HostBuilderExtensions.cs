using Serilog;

namespace CurrencyConverter.Api.DependencyInjection;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseApiSerilog(this IHostBuilder host)
	{
		return host.UseSerilog((context, _, loggerConfiguration) =>
		{
			loggerConfiguration
				.ReadFrom.Configuration(context.Configuration)
				.Enrich.FromLogContext();
		});
	}
}