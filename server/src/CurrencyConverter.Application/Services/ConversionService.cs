using CurrencyConverter.Application.Abstractions.Providers;
using CurrencyConverter.Application.Abstractions.Services;
using CurrencyConverter.Application.DTOs;
using CurrencyConverter.Domain.Requests;
using CurrencyConverter.Domain.ValueObjects;

namespace CurrencyConverter.Application.Services;

public sealed class ConversionService : IConversionService
{
	private readonly ICurrencyProviderFactory _providerFactory;

	public ConversionService(ICurrencyProviderFactory providerFactory)
	{
		_providerFactory = providerFactory;
	}

	public async Task<ConversionDto> ConvertAsync(
		decimal amount,
		string from,
		string to,
		CancellationToken cancellationToken)
	{
		var fromCode = new CurrencyCode(from);
		var toCode = new CurrencyCode(to);

		var request = ConversionRequest.Create(amount, fromCode, toCode);

		var result = await _providerFactory
							.GetProvider()
							.ConvertAsync(request.From.Amount, request.From.Currency, request.To, cancellationToken);

		return new ConversionDto(
			result.Amount,
			result.From.Value,
			result.To.Value,
			result.ConvertedAmount,
			result.RateUsed,
			result.AsOf);
	}
}