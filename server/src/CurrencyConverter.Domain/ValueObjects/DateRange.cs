namespace CurrencyConverter.Domain.ValueObjects;

public readonly record struct DateRange(DateOnly Start, DateOnly End)
{
	public static DateRange Create(DateOnly start, DateOnly end)
	{
		if (end < start)
			throw new ArgumentException("End date must be on or after start date.");

		return new DateRange(start, end);
	}

	public int TotalDaysInclusive() =>
		(End.ToDateTime(TimeOnly.MinValue) - Start.ToDateTime(TimeOnly.MinValue)).Days + 1;
}