using CurrencyConverter.Domain.ValueObjects;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain.ValueObjects;

public sealed class DateRangeTests
{
	[Fact]
	public void Create_WithValidDates_ShouldReturnDateRange()
	{
		var start = new DateOnly(2024, 1, 1);
		var end = new DateOnly(2024, 1, 31);

		var range = DateRange.Create(start, end);

		range.Start.Should().Be(start);
		range.End.Should().Be(end);
	}

	[Fact]
	public void Create_WithSameStartAndEnd_ShouldReturnDateRange()
	{
		var date = new DateOnly(2024, 1, 15);

		var range = DateRange.Create(date, date);

		range.Start.Should().Be(date);
		range.End.Should().Be(date);
	}

	[Fact]
	public void Create_WithEndBeforeStart_ShouldThrowArgumentException()
	{
		var start = new DateOnly(2024, 1, 31);
		var end = new DateOnly(2024, 1, 1);

		var act = () => DateRange.Create(start, end);

		act.Should().Throw<ArgumentException>()
			.WithMessage("End date must be on or after start date.");
	}

	[Theory]
	[InlineData("2024-01-01", "2024-01-01", 1)]
	[InlineData("2024-01-01", "2024-01-31", 31)]
	[InlineData("2024-02-01", "2024-02-29", 29)]
	[InlineData("2024-01-15", "2024-01-20", 6)]
	public void TotalDaysInclusive_ShouldReturnCorrectNumberOfDays(string startStr, string endStr, int expectedDays)
	{
		var start = DateOnly.Parse(startStr);
		var end = DateOnly.Parse(endStr);
		var range = new DateRange(start, end);

		var totalDays = range.TotalDaysInclusive();

		totalDays.Should().Be(expectedDays);
	}
}
