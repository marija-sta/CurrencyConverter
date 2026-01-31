using CurrencyConverter.Application.Abstractions.Observability;

namespace CurrencyConverter.Api.Observability;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
	private static readonly AsyncLocal<string?> Current = new();

	public string? CorrelationId => Current.Value;

	public void Set(string correlationId)
	{
		Current.Value = correlationId;
	}
}