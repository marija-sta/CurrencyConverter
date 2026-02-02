using CurrencyConverter.Api.Middleware;
using CurrencyConverter.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace CurrencyConverter.UnitTests.Api.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
	private readonly ILogger<ExceptionHandlingMiddleware> _logger;
	private readonly ExceptionHandlingMiddleware _middleware;

	public ExceptionHandlingMiddlewareTests()
	{
		_logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
		_middleware = new ExceptionHandlingMiddleware(_logger);
	}

	[Fact]
	public async Task InvokeAsync_WhenNoException_ShouldCallNext()
	{
		var context = new DefaultHttpContext();
		var nextCalled = false;
		RequestDelegate next = _ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		};

		await _middleware.InvokeAsync(context, next);

		nextCalled.Should()
				.BeTrue();
		context.Response.StatusCode.Should()
				.Be(200);
	}

	[Fact]
	public async Task InvokeAsync_WhenDomainValidationException_ShouldReturn400()
	{
		var context = new DefaultHttpContext();
		context.Response.Body = new MemoryStream();

		RequestDelegate next = _ => throw new DomainValidationException("Test validation error");

		await _middleware.InvokeAsync(context, next);

		context.Response.StatusCode.Should().Be(400);
		context.Response.ContentType.Should().Be("application/json");

		context.Response.Body.Seek(0, SeekOrigin.Begin);
		var reader = new StreamReader(context.Response.Body);
		var responseBody = await reader.ReadToEndAsync();
		var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		response.Should().NotBeNull();
		response!.Error.Should().Be("Test validation error");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentException_ShouldReturn400()
	{
		var context = new DefaultHttpContext();
		context.Response.Body = new MemoryStream();

		RequestDelegate next = _ => throw new ArgumentException("Invalid argument");

		await _middleware.InvokeAsync(context, next);

		context.Response.StatusCode.Should().Be(400);

		context.Response.Body.Seek(0, SeekOrigin.Begin);
		var reader = new StreamReader(context.Response.Body);
		var responseBody = await reader.ReadToEndAsync();
		var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		response!.Error.Should().Be("Invalid argument");
	}

	[Fact]
	public async Task InvokeAsync_WhenFormatException_ShouldReturn400()
	{
		var context = new DefaultHttpContext();
		context.Response.Body = new MemoryStream();

		RequestDelegate next = _ => throw new FormatException("Invalid format");

		await _middleware.InvokeAsync(context, next);

		context.Response.StatusCode.Should().Be(400);

		context.Response.Body.Seek(0, SeekOrigin.Begin);
		var reader = new StreamReader(context.Response.Body);
		var responseBody = await reader.ReadToEndAsync();
		var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		response!.Error.Should().Be("Invalid format");
	}

	[Fact]
	public async Task InvokeAsync_WhenUnhandledException_ShouldReturn500()
	{
		var context = new DefaultHttpContext();
		context.Response.Body = new MemoryStream();

		RequestDelegate next = _ => throw new InvalidOperationException("Unexpected error");

		await _middleware.InvokeAsync(context, next);

		context.Response.StatusCode.Should().Be(500);

		context.Response.Body.Seek(0, SeekOrigin.Begin);
		var reader = new StreamReader(context.Response.Body);
		var responseBody = await reader.ReadToEndAsync();
		var response = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		response!.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task InvokeAsync_WhenUnhandledException_ShouldLogError()
	{
		var context = new DefaultHttpContext();
		context.Response.Body = new MemoryStream();
		var exception = new InvalidOperationException("Unexpected error");

		RequestDelegate next = _ => throw exception;

		await _middleware.InvokeAsync(context, next);

		_logger.Received(1)
				.Log(
					LogLevel.Error,
					Arg.Any<EventId>(),
					Arg.Any<object>(),
					exception,
					Arg.Any<Func<object, Exception?, string>>());
	}

	private sealed record ErrorResponse(string Error);
}