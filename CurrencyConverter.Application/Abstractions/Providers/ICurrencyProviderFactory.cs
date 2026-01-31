namespace CurrencyConverter.Application.Abstractions.Providers;

public interface ICurrencyProviderFactory
{
	ICurrencyProvider GetProvider();
}