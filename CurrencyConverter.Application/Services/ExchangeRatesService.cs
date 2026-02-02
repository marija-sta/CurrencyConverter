using CurrencyConverter.Application.Abstractions.Caching;
using CurrencyConverter.Application.Abstractions.Providers;
using CurrencyConverter.Application.Abstractions.Services;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Domain.Paging;
using CurrencyConverter.Domain.ValueObjects;

namespace CurrencyConverter.Application.Services;

public sealed class ExchangeRatesService : IExchangeRatesService
{
    private static readonly TimeSpan LatestTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HistoricalTtl = TimeSpan.FromMinutes(30);

    private readonly ICurrencyProviderFactory _providerFactory;
    private readonly IExchangeRateCache _cache;

    public ExchangeRatesService(ICurrencyProviderFactory providerFactory, IExchangeRateCache cache)
    {
        _providerFactory = providerFactory;
        _cache = cache;
    }

    public async Task<LatestRatesDto> GetLatestAsync(string baseCurrency, CancellationToken cancellationToken)
    {
        var baseCode = new CurrencyCode(baseCurrency);
        var cacheKey = $"latest:{baseCode.Value}";

        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            LatestTtl,
            ct => _providerFactory.GetProvider()
                                  .GetLatestRatesAsync(baseCode, ct),
            cancellationToken);

        return new LatestRatesDto(
            result.BaseCurrency.Value,
            result.AsOf,
            result.Rates.ToDictionary(k => k.Key.Value, v => v.Value));
    }

    public async Task<PagedResult<HistoricalRatePointDto>> GetHistoricalAsync(
        string baseCurrency,
        DateOnly start,
        DateOnly end,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var baseCode = new CurrencyCode(baseCurrency);
        var range = DateRange.Create(start, end);
        var pageRequest = PageRequest.Create(page, pageSize);

        var cacheKey = $"historical:{baseCode.Value}:{range.Start:yyyy-MM-dd}:{range.End:yyyy-MM-dd}";

        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            HistoricalTtl,
            ct => _providerFactory.GetProvider()
                                  .GetHistoricalRatesAsync(baseCode, range, ct),
            cancellationToken);

        var total = result.Points.Count;
        var items = result.Points
                          .OrderBy(p => p.Date)
                          .Skip(pageRequest.Skip())
                          .Take(pageRequest.PageSize)
                          .Select(p => new HistoricalRatePointDto(
                              p.Date,
                              p.Rates.ToDictionary(k => k.Key.Value, v => v.Value)))
                          .ToList();

        return new PagedResult<HistoricalRatePointDto>(items, pageRequest.PageNumber, pageRequest.PageSize, total);
    }
}