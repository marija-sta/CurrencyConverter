using CurrencyConverter.Application.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Infrastructure.Providers;

public sealed class CurrencyProviderFactory : ICurrencyProviderFactory
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ProviderOptions _options;

	public CurrencyProviderFactory(IServiceProvider serviceProvider, IOptions<ProviderOptions> options)
	{
		_serviceProvider = serviceProvider;
		_options = options.Value;
	}

	public ICurrencyProvider GetProvider()
	{
		return this._serviceProvider.GetRequiredKeyedService<ICurrencyProvider>(_options.ActiveProvider);
	}
}