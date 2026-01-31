using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;

namespace CurrencyConverter.IntegrationTests.Api;

public sealed class ConversionControllerTests : IClassFixture<CurrencyConverterWebApplicationFactory>
{
	private readonly CurrencyConverterWebApplicationFactory _factory;

	public ConversionControllerTests(CurrencyConverterWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task Convert_WithValidRequest_ShouldReturn200()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/convert?amount=100&from=USD&to=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var content = await response.Content.ReadFromJsonAsync<ConversionResponse>();
		content.Should().NotBeNull();
		content.Amount.Should().Be(100);
		content.From.Should().Be("USD");
		content.To.Should().Be("EUR");
	}

	[Fact]
	public async Task Convert_WithoutAuthorization_ShouldReturn401()
	{
		var client = _factory.CreateClient();

		var response = await client.GetAsync("/api/v1/convert?amount=100&from=USD&to=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Convert_WithoutRequiredRole_ShouldReturn403()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["rates.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/convert?amount=100&from=USD&to=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task Convert_WithAdminRole_ShouldReturn200()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["admin"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/convert?amount=100&from=USD&to=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Theory]
	[InlineData("TRY")]
	[InlineData("PLN")]
	[InlineData("THB")]
	[InlineData("MXN")]
	public async Task Convert_WithExcludedSourceCurrency_ShouldReturn400(string excludedCurrency)
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync($"/api/v1/convert?amount=100&from={excludedCurrency}&to=USD");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var content = await response.Content.ReadAsStringAsync();
		content.Should().Contain("Currency conversion is not supported for TRY, PLN, THB, or MXN");
	}

	[Theory]
	[InlineData("TRY")]
	[InlineData("PLN")]
	[InlineData("THB")]
	[InlineData("MXN")]
	public async Task Convert_WithExcludedTargetCurrency_ShouldReturn400(string excludedCurrency)
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync($"/api/v1/convert?amount=100&from=USD&to={excludedCurrency}");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var content = await response.Content.ReadAsStringAsync();
		content.Should().Contain("Currency conversion is not supported for TRY, PLN, THB, or MXN");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public async Task Convert_WithZeroOrNegativeAmount_ShouldReturn400(decimal amount)
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync($"/api/v1/convert?amount={amount}&from=USD&to=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		var content = await response.Content.ReadAsStringAsync();
		content.Should().Contain("Amount must be greater than zero");
	}

	[Theory]
	[InlineData("")]
	[InlineData("US")]
	[InlineData("USDT")]
	public async Task Convert_WithInvalidCurrencyCode_ShouldReturn400(string invalidCode)
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync($"/api/v1/convert?amount=100&from={invalidCode}&to=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Convert_ShouldHaveCorrelationIdInResponse()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/convert?amount=100&from=USD&to=EUR");

		response.Headers.Should().ContainKey("X-Correlation-ID");
	}

	[Fact]
	public async Task ApiVersioning_ShouldWorkWithV1Prefix()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/convert?amount=100&from=USD&to=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	private static string GenerateJwtToken(string[] roles)
	{
		var claims = new List<Claim>
		{
			new("sub", "test-user")
		};

		foreach (var role in roles)
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ngFbcjB0f8c+Cz3NmtwFb/rO1VZti67Q5dzQNAsg8hu1ppONbD03IlaF58f05kzFSK4VR7d9MOkaK7QfElqsqw=="));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: "CurrencyConverter",
			audience: "CurrencyConverterClients",
			claims: claims,
			notBefore: DateTime.UtcNow,
			expires: DateTime.UtcNow.AddMinutes(60),
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private sealed record ConversionResponse(decimal Amount, string From, string To, decimal ConvertedAmount, decimal RateUsed, DateOnly AsOf);
}
