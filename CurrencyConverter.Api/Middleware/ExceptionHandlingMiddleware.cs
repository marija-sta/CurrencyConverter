using System.Net;
using System.Text.Json;
using CurrencyConverter.Domain.Exceptions;

namespace CurrencyConverter.Api.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
	private readonly ILogger<ExceptionHandlingMiddleware> _logger;

	public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
	{
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		try
		{
			await next(context);
		}
		catch (DomainValidationException ex)
		{
			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			context.Response.ContentType = "application/json";

			var payload = JsonSerializer.Serialize(new { error = ex.Message });
			await context.Response.WriteAsync(payload);
		}
		catch (ArgumentException ex)
		{
			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			context.Response.ContentType = "application/json";

			var payload = JsonSerializer.Serialize(new { error = ex.Message });
			await context.Response.WriteAsync(payload);
		}
		catch (FormatException ex)
		{
			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			context.Response.ContentType = "application/json";

			var payload = JsonSerializer.Serialize(new { error = ex.Message });
			await context.Response.WriteAsync(payload);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception");

			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			context.Response.ContentType = "application/json";

			var payload = JsonSerializer.Serialize(new { error = "An unexpected error occurred." });
			await context.Response.WriteAsync(payload);
		}
	}
}