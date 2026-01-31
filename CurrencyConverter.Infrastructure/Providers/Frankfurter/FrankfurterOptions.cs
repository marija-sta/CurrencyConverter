namespace CurrencyConverter.Infrastructure.Providers.Frankfurter;

public sealed class FrankfurterOptions
{
	public const string SectionName = "Frankfurter";

	public string BaseUrl { get; init; } = null!;
}