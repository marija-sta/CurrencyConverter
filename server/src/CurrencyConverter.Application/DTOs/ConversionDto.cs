using System.Text.Json.Serialization;

namespace CurrencyConverter.Application.DTOs;

public sealed record ConversionDto(
	[property: JsonPropertyName("amount")] decimal Amount,
	[property: JsonPropertyName("from")] string From,
	[property: JsonPropertyName("to")] string To,
	[property: JsonPropertyName("convertedAmount")] decimal ConvertedAmount,
	[property: JsonPropertyName("rate")] decimal RateUsed,
	[property: JsonPropertyName("date")] DateOnly AsOf);