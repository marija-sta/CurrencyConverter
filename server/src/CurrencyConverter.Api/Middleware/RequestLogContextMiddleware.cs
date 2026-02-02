using System.Security.Claims;
using Serilog.Context;

namespace CurrencyConverter.Api.Middleware;

public sealed class RequestLogContextMiddleware : IMiddleware
{
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		var clientIp = context.Connection.RemoteIpAddress?.ToString();
		var clientId = context.User.FindFirstValue("sub");

		using (LogContext.PushProperty("ClientIp", clientIp))
		using (LogContext.PushProperty("ClientId", clientId))
		{
			await next(context);
		}
	}
}