using CurrencyConverter.Domain.ValueObjects;

namespace CurrencyConverter.Application.Abstractions.Providers;

public interface ICurrencyProvider
{
	Task<LatestRatesResult> GetLatestRatesAsync(CurrencyCode baseCurrency, CancellationToken cancellationToken);

	Task<ConversionProviderResult> ConvertAsync(decimal amount, CurrencyCode from, CurrencyCode to, CancellationToken cancellationToken);

	Task<HistoricalRatesResult> GetHistoricalRatesAsync(
		CurrencyCode baseCurrency,
		DateRange range,
		CancellationToken cancellationToken);
}

public sealed record LatestRatesResult(CurrencyCode BaseCurrency, DateOnly AsOf, IReadOnlyDictionary<CurrencyCode, decimal> Rates);

public sealed record ConversionProviderResult(CurrencyCode From, CurrencyCode To, decimal Amount, decimal ConvertedAmount, decimal RateUsed, DateOnly AsOf);

public sealed record HistoricalRatesResult(CurrencyCode BaseCurrency, DateRange Range, IReadOnlyList<HistoricalRatePoint> Points);

public sealed record HistoricalRatePoint(DateOnly Date, IReadOnlyDictionary<CurrencyCode, decimal> Rates);