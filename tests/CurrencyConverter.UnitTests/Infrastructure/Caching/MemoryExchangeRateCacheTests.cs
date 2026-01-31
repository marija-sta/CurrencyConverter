using CurrencyConverter.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.UnitTests.Infrastructure.Caching;

public sealed class MemoryExchangeRateCacheTests
{
	[Fact]
	public async Task GetOrCreateAsync_WhenCacheEmpty_ShouldCallFactory()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var exchangeRateCache = new MemoryExchangeRateCache(cache);
		var factoryCalled = false;

		var result = await exchangeRateCache.GetOrCreateAsync(
			"test-key",
			TimeSpan.FromMinutes(5),
			_ =>
			{
				factoryCalled = true;
				return Task.FromResult("test-value");
			},
			CancellationToken.None);

		factoryCalled.Should().BeTrue();
		result.Should().Be("test-value");
	}

	[Fact]
	public async Task GetOrCreateAsync_WhenCacheHit_ShouldNotCallFactory()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var exchangeRateCache = new MemoryExchangeRateCache(cache);
		var factoryCallCount = 0;

		await exchangeRateCache.GetOrCreateAsync(
			"test-key",
			TimeSpan.FromMinutes(5),
			_ =>
			{
				factoryCallCount++;
				return Task.FromResult("test-value");
			},
			CancellationToken.None);

		var result = await exchangeRateCache.GetOrCreateAsync(
			"test-key",
			TimeSpan.FromMinutes(5),
			_ =>
			{
				factoryCallCount++;
				return Task.FromResult("different-value");
			},
			CancellationToken.None);

		factoryCallCount.Should().Be(1);
		result.Should().Be("test-value");
	}

	[Fact]
	public async Task GetOrCreateAsync_WithDifferentKeys_ShouldCallFactoryForEach()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var exchangeRateCache = new MemoryExchangeRateCache(cache);

		var result1 = await exchangeRateCache.GetOrCreateAsync(
			"key1",
			TimeSpan.FromMinutes(5),
			_ => Task.FromResult("value1"),
			CancellationToken.None);

		var result2 = await exchangeRateCache.GetOrCreateAsync(
			"key2",
			TimeSpan.FromMinutes(5),
			_ => Task.FromResult("value2"),
			CancellationToken.None);

		result1.Should().Be("value1");
		result2.Should().Be("value2");
	}

	[Fact]
	public async Task GetOrCreateAsync_ShouldRespectTtl()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var exchangeRateCache = new MemoryExchangeRateCache(cache);

		await exchangeRateCache.GetOrCreateAsync(
			"test-key",
			TimeSpan.FromMilliseconds(100),
			_ => Task.FromResult("original-value"),
			CancellationToken.None);

		await Task.Delay(150);

		var factoryCalled = false;
		var result = await exchangeRateCache.GetOrCreateAsync(
			"test-key",
			TimeSpan.FromMinutes(5),
			_ =>
			{
				factoryCalled = true;
				return Task.FromResult("new-value");
			},
			CancellationToken.None);

		factoryCalled.Should().BeTrue();
		result.Should().Be("new-value");
	}
}
