using System.Text.Json.Serialization;

namespace CurrencyConverter.Infrastructure.Providers.Responses;

public sealed class FrankfurterLatestResponse
{
	[JsonPropertyName("amount")]
	public decimal Amount { get; init; }

	[JsonPropertyName("base")]
	public string Base { get; init; } = "";

	[JsonPropertyName("date")]
	public string Date { get; init; } = "";

	[JsonPropertyName("rates")]
	public Dictionary<string, decimal> Rates { get; init; } = new();
}
