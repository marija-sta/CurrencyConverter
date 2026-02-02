using CurrencyConverter.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Infrastructure.Caching;

public sealed class MemoryExchangeRateCache : IExchangeRateCache
{
	private readonly IMemoryCache _cache;

	public MemoryExchangeRateCache(IMemoryCache cache)
	{
		_cache = cache;
	}

	public Task<T> GetOrCreateAsync<T>(
		string key,
		TimeSpan ttl,
		Func<CancellationToken, Task<T>> factory,
		CancellationToken cancellationToken)
	{
		return _cache.GetOrCreateAsync(key, entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = ttl;
			return factory(cancellationToken);
		})!;
	}
}