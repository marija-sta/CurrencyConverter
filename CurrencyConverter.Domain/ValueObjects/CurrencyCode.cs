namespace CurrencyConverter.Domain.ValueObjects;

public readonly record struct CurrencyCode
{
	private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
	{
		"TRY", "PLN", "THB", "MXN"
	};

	public string Value { get; }

	public CurrencyCode(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Currency code is required.");
		}

		var normalized = value.Trim()
							.ToUpperInvariant();
		if (normalized.Length != 3)
		{
			throw new ArgumentException("Currency code must be a 3-letter ISO code.");
		}

		Value = normalized;
	}

	public bool IsExcluded() => Excluded.Contains(Value);

	public override string ToString() => Value;
}