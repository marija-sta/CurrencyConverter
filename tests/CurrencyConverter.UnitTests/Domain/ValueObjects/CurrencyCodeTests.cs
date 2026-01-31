using CurrencyConverter.Domain.ValueObjects;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain.ValueObjects;

public sealed class CurrencyCodeTests
{
	[Fact]
	public void Constructor_WithValidCode_ShouldCreateInstance()
	{
		var code = new CurrencyCode("USD");

		code.Value.Should().Be("USD");
	}

	[Fact]
	public void Constructor_WithLowercaseCode_ShouldNormalizeToUppercase()
	{
		var code = new CurrencyCode("usd");

		code.Value.Should().Be("USD");
	}

	[Fact]
	public void Constructor_WithCodeContainingWhitespace_ShouldTrimAndNormalize()
	{
		var code = new CurrencyCode("  eur  ");

		code.Value.Should().Be("EUR");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_WithNullOrWhitespace_ShouldThrowArgumentException(string? value)
	{
		var act = () => new CurrencyCode(value!);

		act.Should().Throw<ArgumentException>()
			.WithMessage("Currency code is required.");
	}

	[Theory]
	[InlineData("U")]
	[InlineData("US")]
	[InlineData("USDT")]
	[InlineData("DOLLAR")]
	public void Constructor_WithInvalidLength_ShouldThrowArgumentException(string value)
	{
		var act = () => new CurrencyCode(value);

		act.Should().Throw<ArgumentException>()
			.WithMessage("Currency code must be a 3-letter ISO code.");
	}

	[Theory]
	[InlineData("TRY", true)]
	[InlineData("PLN", true)]
	[InlineData("THB", true)]
	[InlineData("MXN", true)]
	[InlineData("USD", false)]
	[InlineData("EUR", false)]
	[InlineData("GBP", false)]
	public void IsExcluded_ShouldReturnCorrectValue(string code, bool expectedExcluded)
	{
		var currencyCode = new CurrencyCode(code);

		currencyCode.IsExcluded().Should().Be(expectedExcluded);
	}

	[Theory]
	[InlineData("try", true)]
	[InlineData("pln", true)]
	[InlineData("thb", true)]
	[InlineData("mxn", true)]
	public void IsExcluded_ShouldBeCaseInsensitive(string code, bool expectedExcluded)
	{
		var currencyCode = new CurrencyCode(code);

		currencyCode.IsExcluded().Should().Be(expectedExcluded);
	}

	[Fact]
	public void ToString_ShouldReturnValue()
	{
		var code = new CurrencyCode("EUR");

		code.ToString().Should().Be("EUR");
	}
}
