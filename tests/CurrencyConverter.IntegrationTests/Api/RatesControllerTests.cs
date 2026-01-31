using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;

namespace CurrencyConverter.IntegrationTests.Api;

public sealed class RatesControllerTests : IClassFixture<CurrencyConverterWebApplicationFactory>
{
	private readonly CurrencyConverterWebApplicationFactory _factory;

	public RatesControllerTests(CurrencyConverterWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetLatest_WithValidRequest_ShouldReturn200()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["rates.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/rates/latest?baseCurrency=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetLatest_WithoutAuthorization_ShouldReturn401()
	{
		var client = _factory.CreateClient();

		var response = await client.GetAsync("/api/v1/rates/latest?baseCurrency=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetLatest_WithoutRequiredRole_ShouldReturn403()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["convert"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/rates/latest?baseCurrency=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task GetLatest_WithAdminRole_ShouldReturn200()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["admin"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/rates/latest?baseCurrency=EUR");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Theory]
	[InlineData("")]
	[InlineData("EU")]
	[InlineData("EURO")]
	public async Task GetLatest_WithInvalidCurrencyCode_ShouldReturn400(string baseCurrency)
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["rates.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync($"/api/v1/rates/latest?baseCurrency={baseCurrency}");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetHistorical_WithValidRequest_ShouldReturn200WithPagination()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["history.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/rates/historical?baseCurrency=EUR&start=2024-01-01&end=2024-01-31&page=1&pageSize=10");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var content = await response.Content.ReadFromJsonAsync<PagedResponse>();
		content.Should().NotBeNull();
		content!.PageNumber.Should().Be(1);
		content.PageSize.Should().Be(10);
	}

	[Fact]
	public async Task GetHistorical_WithoutAuthorization_ShouldReturn401()
	{
		var client = _factory.CreateClient();

		var response = await client.GetAsync("/api/v1/rates/historical?baseCurrency=EUR&start=2024-01-01&end=2024-01-31");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetHistorical_WithEndBeforeStart_ShouldReturn400()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["history.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/rates/historical?baseCurrency=EUR&start=2024-01-31&end=2024-01-01");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public async Task GetHistorical_WithInvalidPageNumber_ShouldReturn400(int page)
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["history.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync($"/api/v1/rates/historical?baseCurrency=EUR&start=2024-01-01&end=2024-01-31&page={page}&pageSize=10");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task Latest_ShouldHaveCorrelationIdInResponse()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["rates.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var response = await client.GetAsync("/api/v1/rates/latest?baseCurrency=EUR");

		response.Headers.Should().ContainKey("X-Correlation-ID");
	}

	[Fact]
	public async Task Latest_ShouldAcceptAndReturnCorrelationId()
	{
		var client = _factory.CreateClient();
		var token = GenerateJwtToken(["rates.read"]);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		var correlationId = Guid.NewGuid().ToString("N");
		client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

		var response = await client.GetAsync("/api/v1/rates/latest?baseCurrency=EUR");

		response.Headers.GetValues("X-Correlation-ID").First().Should().Be(correlationId);
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

	private sealed record PagedResponse(int PageNumber, int PageSize, int TotalItems, int TotalPages);
}
