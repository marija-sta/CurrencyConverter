using System.Text.Json.Serialization;

namespace CurrencyConverter.Application.DTOs;

public sealed record LatestRatesDto(
	[property: JsonPropertyName("baseCurrency")] string Base,
	[property: JsonPropertyName("date")] DateOnly AsOf,
	[property: JsonPropertyName("rates")] IReadOnlyDictionary<string, decimal> Rates);