using CurrencyConverter.Application.Abstractions.Caching;
using CurrencyConverter.Application.Abstractions.Providers;
using CurrencyConverter.Application.Services;
using CurrencyConverter.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Services;

public sealed class ExchangeRatesServiceTests
{
	private readonly ICurrencyProviderFactory _providerFactory;
	private readonly ICurrencyProvider _provider;
	private readonly IExchangeRateCache _cache;
	private readonly ExchangeRatesService _service;

	public ExchangeRatesServiceTests()
	{
		_providerFactory = Substitute.For<ICurrencyProviderFactory>();
		_provider = Substitute.For<ICurrencyProvider>();
		_cache = Substitute.For<IExchangeRateCache>();

		_providerFactory.GetProvider().Returns(_provider);

		_service = new ExchangeRatesService(_providerFactory, _cache);
	}

	[Fact]
	public async Task GetLatestAsync_ShouldUseCacheWithCorrectKey()
	{
		var baseCurrency = "EUR";
		var expectedCacheKey = "latest:EUR";
		var cancellationToken = CancellationToken.None;

		var providerResult = new LatestRatesResult(
			new CurrencyCode("EUR"),
			new DateOnly(2024, 1, 15),
			new Dictionary<CurrencyCode, decimal>
			{
				[new CurrencyCode("USD")] = 1.10m,
				[new CurrencyCode("GBP")] = 0.85m
			});

		_cache.GetOrCreateAsync(
			expectedCacheKey,
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<LatestRatesResult>>>(),
			cancellationToken
		).Returns(providerResult);

		var result = await _service.GetLatestAsync(baseCurrency, cancellationToken);

		await _cache.Received(1).GetOrCreateAsync(
			expectedCacheKey,
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<LatestRatesResult>>>(),
			cancellationToken
		);
	}

	[Fact]
	public async Task GetLatestAsync_ShouldMapProviderResultToDto()
	{
		var baseCurrency = "EUR";
		var cancellationToken = CancellationToken.None;

		var providerResult = new LatestRatesResult(
			new CurrencyCode("EUR"),
			new DateOnly(2024, 1, 15),
			new Dictionary<CurrencyCode, decimal>
			{
				[new CurrencyCode("USD")] = 1.10m,
				[new CurrencyCode("GBP")] = 0.85m
			});

		_cache.GetOrCreateAsync(
			Arg.Any<string>(),
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<LatestRatesResult>>>(),
			cancellationToken
		).Returns(providerResult);

		var result = await _service.GetLatestAsync(baseCurrency, cancellationToken);

		result.Base.Should().Be("EUR");
		result.AsOf.Should().Be(new DateOnly(2024, 1, 15));
		result.Rates.Should().ContainKey("USD").WhoseValue.Should().Be(1.10m);
		result.Rates.Should().ContainKey("GBP").WhoseValue.Should().Be(0.85m);
	}

	[Fact]
	public async Task GetHistoricalAsync_ShouldUseCacheWithCorrectKey()
	{
		var baseCurrency = "EUR";
		var start = new DateOnly(2024, 1, 1);
		var end = new DateOnly(2024, 1, 31);
		var expectedCacheKey = "historical:EUR:2024-01-01:2024-01-31";
		var cancellationToken = CancellationToken.None;

		var providerResult = new HistoricalRatesResult(
			new CurrencyCode("EUR"),
			new DateRange(start, end),
			new List<HistoricalRatePoint>
			{
				new(new DateOnly(2024, 1, 1), new Dictionary<CurrencyCode, decimal>
				{
					[new CurrencyCode("USD")] = 1.10m
				}),
				new(new DateOnly(2024, 1, 2), new Dictionary<CurrencyCode, decimal>
				{
					[new CurrencyCode("USD")] = 1.11m
				})
			});

		_cache.GetOrCreateAsync(
			expectedCacheKey,
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<HistoricalRatesResult>>>(),
			cancellationToken
		).Returns(providerResult);

		await _service.GetHistoricalAsync(baseCurrency, start, end, 1, 10, cancellationToken);

		await _cache.Received(1).GetOrCreateAsync(
			expectedCacheKey,
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<HistoricalRatesResult>>>(),
			cancellationToken
		);
	}

	[Fact]
	public async Task GetHistoricalAsync_ShouldApplyPaginationCorrectly()
	{
		var baseCurrency = "EUR";
		var start = new DateOnly(2024, 1, 1);
		var end = new DateOnly(2024, 1, 31);
		var cancellationToken = CancellationToken.None;

		var points = Enumerable.Range(1, 30)
			.Select(day => new HistoricalRatePoint(
				new DateOnly(2024, 1, day),
				new Dictionary<CurrencyCode, decimal>
				{
					[new CurrencyCode("USD")] = 1.10m + (day * 0.01m)
				}))
			.ToList();

		var providerResult = new HistoricalRatesResult(
			new CurrencyCode("EUR"),
			new DateRange(start, end),
			points);

		_cache.GetOrCreateAsync(
			Arg.Any<string>(),
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<HistoricalRatesResult>>>(),
			cancellationToken
		).Returns(providerResult);

		var result = await _service.GetHistoricalAsync(baseCurrency, start, end, 2, 10, cancellationToken);

		result.PageNumber.Should().Be(2);
		result.PageSize.Should().Be(10);
		result.TotalItems.Should().Be(30);
		result.TotalPages.Should().Be(3);
		result.Items.Should().HaveCount(10);
		result.Items.First().Date.Should().Be(new DateOnly(2024, 1, 11));
		result.Items.Last().Date.Should().Be(new DateOnly(2024, 1, 20));
	}

	[Fact]
	public async Task GetHistoricalAsync_ShouldOrderByDateAscending()
	{
		var baseCurrency = "EUR";
		var start = new DateOnly(2024, 1, 1);
		var end = new DateOnly(2024, 1, 5);
		var cancellationToken = CancellationToken.None;

		var points = new List<HistoricalRatePoint>
		{
			new(new DateOnly(2024, 1, 3), new Dictionary<CurrencyCode, decimal>()),
			new(new DateOnly(2024, 1, 1), new Dictionary<CurrencyCode, decimal>()),
			new(new DateOnly(2024, 1, 5), new Dictionary<CurrencyCode, decimal>()),
			new(new DateOnly(2024, 1, 2), new Dictionary<CurrencyCode, decimal>()),
			new(new DateOnly(2024, 1, 4), new Dictionary<CurrencyCode, decimal>())
		};

		var providerResult = new HistoricalRatesResult(
			new CurrencyCode("EUR"),
			new DateRange(start, end),
			points);

		_cache.GetOrCreateAsync(
			Arg.Any<string>(),
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<HistoricalRatesResult>>>(),
			cancellationToken
		).Returns(providerResult);

		var result = await _service.GetHistoricalAsync(baseCurrency, start, end, 1, 10, cancellationToken);

		result.Items.Should().HaveCount(5);
		result.Items.Select(x => x.Date).Should().BeInAscendingOrder();
		result.Items.First().Date.Should().Be(new DateOnly(2024, 1, 1));
		result.Items.Last().Date.Should().Be(new DateOnly(2024, 1, 5));
	}

	[Fact]
	public async Task GetHistoricalAsync_ShouldMapRatesCorrectly()
	{
		var baseCurrency = "EUR";
		var start = new DateOnly(2024, 1, 1);
		var end = new DateOnly(2024, 1, 1);
		var cancellationToken = CancellationToken.None;

		var providerResult = new HistoricalRatesResult(
			new CurrencyCode("EUR"),
			new DateRange(start, end),
			new List<HistoricalRatePoint>
			{
				new(new DateOnly(2024, 1, 1), new Dictionary<CurrencyCode, decimal>
				{
					[new CurrencyCode("USD")] = 1.10m,
					[new CurrencyCode("GBP")] = 0.85m
				})
			});

		_cache.GetOrCreateAsync(
			Arg.Any<string>(),
			Arg.Any<TimeSpan>(),
			Arg.Any<Func<CancellationToken, Task<HistoricalRatesResult>>>(),
			cancellationToken
		).Returns(providerResult);

		var result = await _service.GetHistoricalAsync(baseCurrency, start, end, 1, 10, cancellationToken);

		result.Items.Should().HaveCount(1);
		var item = result.Items.First();
		item.Date.Should().Be(new DateOnly(2024, 1, 1));
		item.Rates.Should().ContainKey("USD").WhoseValue.Should().Be(1.10m);
		item.Rates.Should().ContainKey("GBP").WhoseValue.Should().Be(0.85m);
	}
}
