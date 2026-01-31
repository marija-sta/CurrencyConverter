namespace CurrencyConverter.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, CurrencyCode Currency);