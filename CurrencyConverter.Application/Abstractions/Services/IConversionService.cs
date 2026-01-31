namespace CurrencyConverter.Application.Services;

public interface IConversionService
{
	Task<ConversionDto> ConvertAsync(decimal amount, string from, string to, CancellationToken cancellationToken);
}

public sealed record ConversionDto(
	decimal Amount,
	string From,
	string To,
	decimal ConvertedAmount,
	decimal RateUsed,
	DateOnly AsOf);