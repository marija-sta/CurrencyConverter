using System.Net;
using Polly;
using CurrencyConverter.Infrastructure.Helpers;
using FluentAssertions;

namespace CurrencyConverter.IntegrationTests.Infrastructure.Helpers;

public sealed class FrankfurterTransientErrorClassifierTests
{
	[Fact]
	public void IsTransient_WithException_ShouldReturnTrue()
	{
		var outcome = Outcome.FromException<HttpResponseMessage>(new HttpRequestException("Network error"));

		var result = FrankfurterTransientErrorClassifier.IsTransient(outcome);

		result.Should().BeTrue();
	}

	[Theory]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.BadGateway)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	[InlineData(HttpStatusCode.GatewayTimeout)]
	public void IsTransient_With5xxStatusCode_ShouldReturnTrue(HttpStatusCode statusCode)
	{
		var response = new HttpResponseMessage(statusCode);
		var outcome = Outcome.FromResult(response);

		var result = FrankfurterTransientErrorClassifier.IsTransient(outcome);

		result.Should().BeTrue();
	}

	[Fact]
	public void IsTransient_With408RequestTimeout_ShouldReturnTrue()
	{
		var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
		var outcome = Outcome.FromResult(response);

		var result = FrankfurterTransientErrorClassifier.IsTransient(outcome);

		result.Should().BeTrue();
	}

	[Fact]
	public void IsTransient_With429TooManyRequests_ShouldReturnTrue()
	{
		var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
		var outcome = Outcome.FromResult(response);

		var result = FrankfurterTransientErrorClassifier.IsTransient(outcome);

		result.Should().BeTrue();
	}

	[Theory]
	[InlineData(HttpStatusCode.OK)]
	[InlineData(HttpStatusCode.Created)]
	[InlineData(HttpStatusCode.NoContent)]
	public void IsTransient_With2xxStatusCode_ShouldReturnFalse(HttpStatusCode statusCode)
	{
		var response = new HttpResponseMessage(statusCode);
		var outcome = Outcome.FromResult(response);

		var result = FrankfurterTransientErrorClassifier.IsTransient(outcome);

		result.Should().BeFalse();
	}

	[Theory]
	[InlineData(HttpStatusCode.BadRequest)]
	[InlineData(HttpStatusCode.Unauthorized)]
	[InlineData(HttpStatusCode.Forbidden)]
	[InlineData(HttpStatusCode.NotFound)]
	[InlineData(HttpStatusCode.MethodNotAllowed)]
	public void IsTransient_With4xxStatusCodeExcept408And429_ShouldReturnFalse(HttpStatusCode statusCode)
	{
		var response = new HttpResponseMessage(statusCode);
		var outcome = Outcome.FromResult(response);

		var result = FrankfurterTransientErrorClassifier.IsTransient(outcome);

		result.Should().BeFalse();
	}

	[Fact]
	public void IsTransient_WithNullResult_ShouldReturnTrue()
	{
		var outcome = Outcome.FromResult<HttpResponseMessage>(null!);

		var result = FrankfurterTransientErrorClassifier.IsTransient(outcome);

		result.Should().BeTrue();
	}
}
