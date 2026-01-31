using CurrencyConverter.Application.Abstractions.Providers;
using CurrencyConverter.Application.Services;
using CurrencyConverter.Domain.Exceptions;
using CurrencyConverter.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Application.Services;

public sealed class ConversionServiceTests
{
	private readonly ICurrencyProviderFactory _providerFactory;
	private readonly ICurrencyProvider _provider;
	private readonly ConversionService _service;

	public ConversionServiceTests()
	{
		_providerFactory = Substitute.For<ICurrencyProviderFactory>();
		_provider = Substitute.For<ICurrencyProvider>();

		_providerFactory.GetProvider().Returns(_provider);

		_service = new ConversionService(_providerFactory);
	}

	[Fact]
	public async Task ConvertAsync_WithValidInputs_ShouldCallProviderAndMapResult()
	{
		var amount = 100m;
		var from = "USD";
		var to = "EUR";
		var cancellationToken = CancellationToken.None;

		var providerResult = new ConversionProviderResult(
			new CurrencyCode("USD"),
			new CurrencyCode("EUR"),
			100m,
			85m,
			0.85m,
			new DateOnly(2024, 1, 15));

		_provider.ConvertAsync(
			amount,
			Arg.Is<CurrencyCode>(c => c.Value == "USD"),
			Arg.Is<CurrencyCode>(c => c.Value == "EUR"),
			cancellationToken
		).Returns(providerResult);

		var result = await _service.ConvertAsync(amount, from, to, cancellationToken);

		result.Amount.Should().Be(100m);
		result.From.Should().Be("USD");
		result.To.Should().Be("EUR");
		result.ConvertedAmount.Should().Be(85m);
		result.RateUsed.Should().Be(0.85m);
		result.AsOf.Should().Be(new DateOnly(2024, 1, 15));
	}

	[Theory]
	[InlineData("TRY")]
	[InlineData("PLN")]
	[InlineData("THB")]
	[InlineData("MXN")]
	public async Task ConvertAsync_WithExcludedSourceCurrency_ShouldThrowDomainValidationException(string excludedCurrency)
	{
		var act = async () => await _service.ConvertAsync(100m, excludedCurrency, "USD", CancellationToken.None);

		await act.Should().ThrowAsync<DomainValidationException>()
			.WithMessage("Currency conversion is not supported for TRY, PLN, THB, or MXN.");
	}

	[Theory]
	[InlineData("TRY")]
	[InlineData("PLN")]
	[InlineData("THB")]
	[InlineData("MXN")]
	public async Task ConvertAsync_WithExcludedTargetCurrency_ShouldThrowDomainValidationException(string excludedCurrency)
	{
		var act = async () => await _service.ConvertAsync(100m, "USD", excludedCurrency, CancellationToken.None);

		await act.Should().ThrowAsync<DomainValidationException>()
			.WithMessage("Currency conversion is not supported for TRY, PLN, THB, or MXN.");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public async Task ConvertAsync_WithZeroOrNegativeAmount_ShouldThrowDomainValidationException(decimal amount)
	{
		var act = async () => await _service.ConvertAsync(amount, "USD", "EUR", CancellationToken.None);

		await act.Should().ThrowAsync<DomainValidationException>()
			.WithMessage("Amount must be greater than zero.");
	}

	[Theory]
	[InlineData("")]
	[InlineData("US")]
	[InlineData("USDT")]
	public async Task ConvertAsync_WithInvalidCurrencyCode_ShouldThrowArgumentException(string invalidCode)
	{
		var act = async () => await _service.ConvertAsync(100m, invalidCode, "EUR", CancellationToken.None);

		await act.Should().ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task ConvertAsync_ShouldNormalizeCurrencyCodeCase()
	{
		var cancellationToken = CancellationToken.None;

		var providerResult = new ConversionProviderResult(
			new CurrencyCode("USD"),
			new CurrencyCode("EUR"),
			100m,
			85m,
			0.85m,
			new DateOnly(2024, 1, 15));

		_provider.ConvertAsync(
			Arg.Any<decimal>(),
			Arg.Any<CurrencyCode>(),
			Arg.Any<CurrencyCode>(),
			cancellationToken
		).Returns(providerResult);

		var result = await _service.ConvertAsync(100m, "usd", "eur", cancellationToken);

		result.From.Should().Be("USD");
		result.To.Should().Be("EUR");
	}
}
