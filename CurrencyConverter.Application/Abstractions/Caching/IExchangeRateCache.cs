namespace CurrencyConverter.Application.Abstractions.Caching;

public interface IExchangeRateCache
{
	Task<T> GetOrCreateAsync<T>(
		string key,
		TimeSpan ttl,
		Func<CancellationToken, Task<T>> factory,
		CancellationToken cancellationToken);
}