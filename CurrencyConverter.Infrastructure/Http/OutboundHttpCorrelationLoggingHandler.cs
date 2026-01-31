using System.Diagnostics;
using CurrencyConverter.Application.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Infrastructure.Http;

public sealed class OutboundHttpCorrelationLoggingHandler : DelegatingHandler
{
	private const string CorrelationHeaderName = "X-Correlation-ID";

	private readonly ICorrelationIdAccessor correlationIdAccessor;
	private readonly ILogger<OutboundHttpCorrelationLoggingHandler> logger;

	public OutboundHttpCorrelationLoggingHandler(
		ICorrelationIdAccessor correlationIdAccessor,
		ILogger<OutboundHttpCorrelationLoggingHandler> logger)
	{
		this.correlationIdAccessor = correlationIdAccessor;
		this.logger = logger;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var correlationId = correlationIdAccessor.CorrelationId;

		if (!string.IsNullOrWhiteSpace(correlationId))
		{
			request.Headers.TryAddWithoutValidation(CorrelationHeaderName, correlationId);
		}

		var stopwatch = Stopwatch.StartNew();

		try
		{
			var response = await base.SendAsync(request, cancellationToken);
			stopwatch.Stop();

			logger.LogInformation(
				"Outbound HTTP call completed {Method} {Url} {StatusCode} in {ElapsedMs}ms",
				request.Method.Method,
				request.RequestUri?.ToString(),
				(int)response.StatusCode,
				stopwatch.ElapsedMilliseconds);

			return response;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();

			logger.LogWarning(
				ex,
				"Outbound HTTP call failed {Method} {Url} in {ElapsedMs}ms",
				request.Method.Method,
				request.RequestUri?.ToString(),
				stopwatch.ElapsedMilliseconds);

			throw;
		}
	}
}