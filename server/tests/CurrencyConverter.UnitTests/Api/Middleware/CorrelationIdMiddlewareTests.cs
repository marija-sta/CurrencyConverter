using CurrencyConverter.Api.Middleware;
using CurrencyConverter.Application.Abstractions.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Api.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
	[Fact]
	public async Task InvokeAsync_WhenNoCorrelationIdInRequest_ShouldGenerateNew()
	{
		var accessor = Substitute.For<ICorrelationIdAccessor>();
		var middleware = new CorrelationIdMiddleware(accessor);
		var context = new DefaultHttpContext();
		var nextCalled = false;
		RequestDelegate next = _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		};

		await middleware.InvokeAsync(context, next);

		nextCalled.Should().BeTrue();
		accessor.Received(1).Set(Arg.Is<string>(id => !string.IsNullOrWhiteSpace(id)));
		context.Response.Headers.Should().ContainKey("X-Correlation-ID");
	}

	[Fact]
	public async Task InvokeAsync_WhenCorrelationIdInRequest_ShouldUseExisting()
	{
		var accessor = Substitute.For<ICorrelationIdAccessor>();
		var middleware = new CorrelationIdMiddleware(accessor);
		var context = new DefaultHttpContext();
		var existingId = "existing-correlation-id";
		context.Request.Headers["X-Correlation-ID"] = existingId;

		RequestDelegate next = _ => Task.CompletedTask;

		await middleware.InvokeAsync(context, next);

		accessor.Received(1).Set(existingId);
		context.Response.Headers["X-Correlation-ID"].ToString().Should().Be(existingId);
	}

	[Fact]
	public async Task InvokeAsync_ShouldAddCorrelationIdToResponseHeader()
	{
		var accessor = Substitute.For<ICorrelationIdAccessor>();
		var middleware = new CorrelationIdMiddleware(accessor);
		var context = new DefaultHttpContext();

		RequestDelegate next = _ => Task.CompletedTask;

		await middleware.InvokeAsync(context, next);

		context.Response.Headers.Should().ContainKey("X-Correlation-ID");
		var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
		correlationId.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task InvokeAsync_WithEmptyCorrelationIdHeader_ShouldGenerateNew()
	{
		var accessor = Substitute.For<ICorrelationIdAccessor>();
		var middleware = new CorrelationIdMiddleware(accessor);
		var context = new DefaultHttpContext();
		context.Request.Headers["X-Correlation-ID"] = "   ";

		RequestDelegate next = _ => Task.CompletedTask;

		await middleware.InvokeAsync(context, next);

		accessor.Received(1).Set(Arg.Is<string>(id => !string.IsNullOrWhiteSpace(id) && id != "   "));
	}
}