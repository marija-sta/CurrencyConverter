using CurrencyConverter.Application.DTOs;

namespace CurrencyConverter.Application.Abstractions.Services;

public interface IConversionService
{
	Task<ConversionDto> ConvertAsync(
		decimal amount,
		string from,
		string to,
		CancellationToken cancellationToken);
}