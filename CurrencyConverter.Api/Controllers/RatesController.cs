using Asp.Versioning;
using CurrencyConverter.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/rates")]
public sealed class RatesController : ControllerBase
{
	private readonly IExchangeRatesService _service;

	public RatesController(IExchangeRatesService service)
	{
		this._service = service;
	}

	[Authorize(Policy = "RatesRead")]
	[HttpGet("latest")]
	public async Task<ActionResult<LatestRatesDto>> Latest(
		[FromQuery] string baseCurrency,
		CancellationToken cancellationToken)
	{
		var result = await this._service.GetLatestAsync(baseCurrency, cancellationToken);
		return Ok(result);
	}

	[Authorize(Policy = "HistoryRead")]
	[HttpGet("historical")]
	public async Task<ActionResult<object>> Historical(
		[FromQuery] string baseCurrency,
		[FromQuery] DateOnly start,
		[FromQuery] DateOnly end,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 30,
		CancellationToken cancellationToken = default)
	{
		var result =
			await this._service.GetHistoricalAsync(baseCurrency, start, end, page, pageSize, cancellationToken);

		return Ok(new
		{
			result.PageNumber,
			result.PageSize,
			result.TotalItems,
			result.TotalPages,
			Items = result.Items
		});
	}
}