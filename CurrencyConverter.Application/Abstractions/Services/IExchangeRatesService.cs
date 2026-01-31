using CurrencyConverter.Domain.Paging;

namespace CurrencyConverter.Application.Services;

public interface IExchangeRatesService
{
	Task<LatestRatesDto> GetLatestAsync(string baseCurrency, CancellationToken cancellationToken);

	Task<PagedResult<HistoricalRatePointDto>> GetHistoricalAsync(
		string baseCurrency,
		DateOnly start,
		DateOnly end,
		int page,
		int pageSize,
		CancellationToken cancellationToken);
}

public sealed record LatestRatesDto(string Base, DateOnly AsOf, IReadOnlyDictionary<string, decimal> Rates);

public sealed record HistoricalRatePointDto(DateOnly Date, IReadOnlyDictionary<string, decimal> Rates);