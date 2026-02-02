namespace CurrencyConverter.Infrastructure.Options;

public sealed class FrankfurterOptions
{
	public const string SectionName = "Frankfurter";

	public string BaseUrl { get; init; } = null!;
}