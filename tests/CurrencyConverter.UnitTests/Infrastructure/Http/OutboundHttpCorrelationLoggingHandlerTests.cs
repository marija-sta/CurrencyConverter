using System.Net;
using CurrencyConverter.Application.Abstractions.Observability;
using CurrencyConverter.Infrastructure.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Infrastructure.Http;

public sealed class OutboundHttpCorrelationLoggingHandlerTests
{
	[Fact]
	public async Task SendAsync_WhenRequestSucceeds_ShouldLogSuccess()
	{
		var logger = Substitute.For<ILogger<OutboundHttpCorrelationLoggingHandler>>();
		var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
		correlationIdAccessor.CorrelationId.Returns("test-correlation-id");

		var handler = new OutboundHttpCorrelationLoggingHandler(correlationIdAccessor, logger);
		handler.InnerHandler = new TestHttpMessageHandler(HttpStatusCode.OK);

		var invoker = new HttpMessageInvoker(handler);
		var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

		var response = await invoker.SendAsync(request, CancellationToken.None);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		request.Headers.Should().ContainKey("X-Correlation-ID");
		request.Headers.GetValues("X-Correlation-ID").First().Should().Be("test-correlation-id");

		logger.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Outbound HTTP call completed")),
			null,
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task SendAsync_WhenCorrelationIdIsNull_ShouldNotAddHeader()
	{
		var logger = Substitute.For<ILogger<OutboundHttpCorrelationLoggingHandler>>();
		var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
		correlationIdAccessor.CorrelationId.Returns((string?)null);

		var handler = new OutboundHttpCorrelationLoggingHandler(correlationIdAccessor, logger);
		handler.InnerHandler = new TestHttpMessageHandler(HttpStatusCode.OK);

		var invoker = new HttpMessageInvoker(handler);
		var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

		await invoker.SendAsync(request, CancellationToken.None);

		request.Headers.Should().NotContainKey("X-Correlation-ID");
	}

	[Fact]
	public async Task SendAsync_WhenRequestFails_ShouldLogWarningAndRethrow()
	{
		var logger = Substitute.For<ILogger<OutboundHttpCorrelationLoggingHandler>>();
		var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();

		var handler = new OutboundHttpCorrelationLoggingHandler(correlationIdAccessor, logger);
		handler.InnerHandler = new TestHttpMessageHandler(new HttpRequestException("Network error"));

		var invoker = new HttpMessageInvoker(handler);
		var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

		var act = async () => await invoker.SendAsync(request, CancellationToken.None);

		await act.Should().ThrowAsync<HttpRequestException>().WithMessage("Network error");

		logger.Received(1).Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Outbound HTTP call failed")),
			Arg.Any<HttpRequestException>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	[Fact]
	public async Task SendAsync_WhenInnerHandlerThrowsException_ShouldLogWithElapsedTime()
	{
		var logger = Substitute.For<ILogger<OutboundHttpCorrelationLoggingHandler>>();
		var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();

		var handler = new OutboundHttpCorrelationLoggingHandler(correlationIdAccessor, logger);
		handler.InnerHandler = new TestHttpMessageHandler(new InvalidOperationException("Unexpected error"));

		var invoker = new HttpMessageInvoker(handler);
		var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/test");

		var act = async () => await invoker.SendAsync(request, CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Unexpected error");

		logger.Received(1).Log(
			LogLevel.Warning,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("Outbound HTTP call failed") && o.ToString()!.Contains("POST")),
			Arg.Any<InvalidOperationException>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	private sealed class TestHttpMessageHandler : HttpMessageHandler
	{
		private readonly HttpStatusCode? _statusCode;
		private readonly Exception? _exception;

		public TestHttpMessageHandler(HttpStatusCode statusCode)
		{
			_statusCode = statusCode;
		}

		public TestHttpMessageHandler(Exception exception)
		{
			_exception = exception;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (_exception is not null)
			{
				throw _exception;
			}

			return Task.FromResult(new HttpResponseMessage(_statusCode!.Value));
		}
	}
}
