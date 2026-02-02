using System.Text.Json.Serialization;

namespace CurrencyConverter.Application.DTOs;

public sealed record HistoricalRatesResponseDto(
	[property: JsonPropertyName("baseCurrency")] string BaseCurrency,
	[property: JsonPropertyName("startDate")] string StartDate,
	[property: JsonPropertyName("endDate")] string EndDate,
	[property: JsonPropertyName("rates")] IReadOnlyList<HistoricalRatePointDto> Rates,
	[property: JsonPropertyName("page")] int Page,
	[property: JsonPropertyName("pageSize")] int PageSize,
	[property: JsonPropertyName("totalItems")] int TotalItems,
	[property: JsonPropertyName("totalPages")] int TotalPages);