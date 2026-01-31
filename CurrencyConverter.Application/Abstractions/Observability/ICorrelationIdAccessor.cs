namespace CurrencyConverter.Application.Abstractions.Observability;

public interface ICorrelationIdAccessor
{
	string? CorrelationId { get; }
	void Set(string correlationId);
}