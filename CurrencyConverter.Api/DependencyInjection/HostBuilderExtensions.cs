using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace CurrencyConverter.Api.DependencyInjection;

[ExcludeFromCodeCoverage]
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