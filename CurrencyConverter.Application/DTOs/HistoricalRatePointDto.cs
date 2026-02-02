using System.Text.Json.Serialization;

namespace CurrencyConverter.Application.DTOs;

public sealed record HistoricalRatePointDto(
	[property: JsonPropertyName("date")] DateOnly Date,
	[property: JsonPropertyName("rates")] IReadOnlyDictionary<string, decimal> Rates);