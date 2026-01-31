using Asp.Versioning;
using CurrencyConverter.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api.Controllers.V1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/convert")]
public sealed class ConversionController : ControllerBase
{
	private readonly IConversionService _service;

	public ConversionController(IConversionService service)
	{
		_service = service;
	}

	[HttpGet]
	public async Task<ActionResult<ConversionDto>> Convert(
		[FromQuery] decimal amount,
		[FromQuery] string from,
		[FromQuery] string to,
		CancellationToken cancellationToken)
	{
		var result = await _service.ConvertAsync(amount, from, to, cancellationToken);
		return Ok(result);
	}
}