using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.Requests;
using CurrencyConverter.Domain.ValueObjects;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain.Requests;

public sealed class ConversionRequestTests
{
	[Fact]
	public void Create_WithValidInputs_ShouldReturnConversionRequest()
	{
		var from = new CurrencyCode("USD");
		var to = new CurrencyCode("EUR");

		var request = ConversionRequest.Create(100m, from, to);

		request.From.Amount.Should().Be(100m);
		request.From.Currency.Should().Be(from);
		request.To.Should().Be(to);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100.50)]
	public void Create_WithZeroOrNegativeAmount_ShouldThrowDomainValidationException(decimal amount)
	{
		var from = new CurrencyCode("USD");
		var to = new CurrencyCode("EUR");

		var act = () => ConversionRequest.Create(amount, from, to);

		act.Should().Throw<DomainValidationException>()
			.WithMessage("Amount must be greater than zero.");
	}

	[Theory]
	[InlineData("TRY")]
	[InlineData("PLN")]
	[InlineData("THB")]
	[InlineData("MXN")]
	public void Create_WithExcludedSourceCurrency_ShouldThrowDomainValidationException(string excludedCode)
	{
		var from = new CurrencyCode(excludedCode);
		var to = new CurrencyCode("USD");

		var act = () => ConversionRequest.Create(100m, from, to);

		act.Should().Throw<DomainValidationException>()
			.WithMessage("Currency conversion is not supported for TRY, PLN, THB, or MXN.");
	}

	[Theory]
	[InlineData("TRY")]
	[InlineData("PLN")]
	[InlineData("THB")]
	[InlineData("MXN")]
	public void Create_WithExcludedTargetCurrency_ShouldThrowDomainValidationException(string excludedCode)
	{
		var from = new CurrencyCode("USD");
		var to = new CurrencyCode(excludedCode);

		var act = () => ConversionRequest.Create(100m, from, to);

		act.Should().Throw<DomainValidationException>()
			.WithMessage("Currency conversion is not supported for TRY, PLN, THB, or MXN.");
	}

	[Fact]
	public void Create_WithBothCurrenciesExcluded_ShouldThrowDomainValidationException()
	{
		var from = new CurrencyCode("TRY");
		var to = new CurrencyCode("PLN");

		var act = () => ConversionRequest.Create(100m, from, to);

		act.Should().Throw<DomainValidationException>()
			.WithMessage("Currency conversion is not supported for TRY, PLN, THB, or MXN.");
	}
}
