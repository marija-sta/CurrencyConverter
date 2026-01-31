using CurrencyConverter.Application.Abstractions.Observability;
using Serilog.Context;

namespace CurrencyConverter.Api.Middleware;

public sealed class CorrelationIdMiddleware : IMiddleware
{
	public const string HeaderName = "X-Correlation-ID";

	private readonly ICorrelationIdAccessor _correlationIdAccessor;

	public CorrelationIdMiddleware(ICorrelationIdAccessor correlationIdAccessor)
	{
		this._correlationIdAccessor = correlationIdAccessor;
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		var correlationId = GetOrCreateCorrelationId(context);

		this._correlationIdAccessor.Set(correlationId);

		context.Response.OnStarting(() =>
		{
			context.Response.Headers[HeaderName] = correlationId;
			return Task.CompletedTask;
		});

		using (LogContext.PushProperty("CorrelationId", correlationId))
		{
			await next(context);
		}
	}

	private static string GetOrCreateCorrelationId(HttpContext context)
	{
		if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
		{
			var value = headerValue.ToString();
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}

		return Guid.NewGuid().ToString("N");
	}
}