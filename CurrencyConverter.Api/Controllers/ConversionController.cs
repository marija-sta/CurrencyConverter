using Asp.Versioning;
using CurrencyConverter.Application.Abstractions.Services;
using CurrencyConverter.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/convert")]
public sealed class ConversionController : ControllerBase
{
	private readonly IConversionService _service;

	public ConversionController(IConversionService service)
	{
		this._service = service;
	}

	[Authorize(Policy = "Convert")]
	[HttpGet]
	public async Task<ActionResult<ConversionDto>> Convert(
		[FromQuery] decimal amount,
		[FromQuery] string from,
		[FromQuery] string to,
		CancellationToken cancellationToken)
	{
		var result = await this._service.ConvertAsync(amount, from, to, cancellationToken);
		return Ok(result);
	}
}