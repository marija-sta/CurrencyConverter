using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Domain.Paging;

namespace CurrencyConverter.Application.Abstractions.Services;

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