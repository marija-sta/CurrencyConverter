using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.ValueObjects;

namespace CurrencyConverter.Domain.Requests;

/// <summary>
/// Represents a currency conversion request with validated business rules.
/// Restricted currencies (TRY, PLN, THB, MXN) cannot be used as source or target.
/// Note: This restriction applies only to conversion operations, not to exchange rate retrieval endpoints.
/// </summary>
public sealed record ConversionRequest(Money From, CurrencyCode To)
{
	public static ConversionRequest Create(decimal amount, CurrencyCode from, CurrencyCode to)
	{
		if (amount <= 0)
			throw new DomainValidationException("Amount must be greater than zero.");

		if (from.IsExcluded() || to.IsExcluded())
			throw new DomainValidationException("Currency conversion is not supported for TRY, PLN, THB, or MXN.");

		return new ConversionRequest(new Money(amount, from), to);
	}
}