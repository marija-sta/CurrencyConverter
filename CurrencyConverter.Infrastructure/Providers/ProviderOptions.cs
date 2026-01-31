using CurrencyConverter.Application.Abstractions.Providers;

namespace CurrencyConverter.Infrastructure.Providers;

public sealed class ProviderOptions
{
	public const string SectionName = "CurrencyProviders";

	public CurrencyProviderKey ActiveProvider { get; init; } = CurrencyProviderKey.Frankfurter;
}