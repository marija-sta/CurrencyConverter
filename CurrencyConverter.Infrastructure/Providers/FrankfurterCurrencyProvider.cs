using System.Globalization;
using System.Net.Http.Json;
using CurrencyConverter.Application.Abstractions.Providers;
using CurrencyConverter.Domain.ValueObjects;
using CurrencyConverter.Infrastructure.Providers.Responses;

namespace CurrencyConverter.Infrastructure.Providers;

public sealed class FrankfurterCurrencyProvider : ICurrencyProvider
{
    private readonly HttpClient _httpClient;

    public FrankfurterCurrencyProvider(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    public async Task<LatestRatesResult> GetLatestRatesAsync(
        CurrencyCode baseCurrency,
        CancellationToken cancellationToken)
    {
        var url = $"latest?base={baseCurrency.Value}";
        var response = await this._httpClient.GetFromJsonAsync<FrankfurterLatestResponse>(url, cancellationToken)
                       ?? throw new InvalidOperationException("Frankfurter response was empty.");

        return new LatestRatesResult(
            new CurrencyCode(response.Base),
            DateOnly.ParseExact(response.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            response.Rates.ToDictionary(k => new CurrencyCode(k.Key), v => v.Value));
    }

    public async Task<ConversionProviderResult> ConvertAsync(
        decimal amount,
        CurrencyCode from,
        CurrencyCode to,
        CancellationToken cancellationToken)
    {
        var amountStr = amount.ToString(CultureInfo.InvariantCulture);
        var url = $"latest?amount={amountStr}&from={from.Value}&to={to.Value}";

        var response = await this._httpClient.GetFromJsonAsync<FrankfurterLatestResponse>(url, cancellationToken)
                       ?? throw new InvalidOperationException("Frankfurter response was empty.");

        if (!response.Rates.TryGetValue(to.Value, out var converted))
            throw new InvalidOperationException("Frankfurter response did not include the requested target currency.");

        var rateUsed = amount == 0 ? 0 : converted / amount;

        return new ConversionProviderResult(
            from,
            to,
            amount,
            converted,
            rateUsed,
            DateOnly.ParseExact(response.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    public async Task<HistoricalRatesResult> GetHistoricalRatesAsync(
        CurrencyCode baseCurrency,
        DateRange range,
        CancellationToken cancellationToken)
    {
        var url = $"{range.Start:yyyy-MM-dd}..{range.End:yyyy-MM-dd}?base={baseCurrency.Value}";
        var response = await this._httpClient.GetFromJsonAsync<FrankfurterHistoricalResponse>(url, cancellationToken)
                       ?? throw new InvalidOperationException("Frankfurter response was empty.");

        var points = response.Rates
                             .Select(kvp => new HistoricalRatePoint(
                                 DateOnly.ParseExact(kvp.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                                 kvp.Value.ToDictionary(k => new CurrencyCode(k.Key), v => v.Value)))
                             .ToList();

        return new HistoricalRatesResult(
            new CurrencyCode(response.Base),
            new DateRange(
                DateOnly.ParseExact(response.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(response.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
            points);
    }
}

