using System.Net;
using System.Text;
using CurrencyConverter.Domain.ValueObjects;
using CurrencyConverter.Infrastructure.Providers;
using FluentAssertions;

namespace CurrencyConverter.IntegrationTests.Infrastructure.Providers;

public sealed class FrankfurterCurrencyProviderTests
{
	[Fact]
	public async Task GetLatestRatesAsync_WithValidResponse_ShouldParseCorrectly()
	{
		var json = """
		{
			"amount": 1,
			"base": "EUR",
			"date": "2024-01-15",
			"rates": {
				"USD": 1.10,
				"GBP": 0.85
			}
		}
		""";

		var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
		var provider = new FrankfurterCurrencyProvider(httpClient);

		var result = await provider.GetLatestRatesAsync(new CurrencyCode("EUR"), CancellationToken.None);

		result.BaseCurrency.Value.Should().Be("EUR");
		result.AsOf.Should().Be(new DateOnly(2024, 1, 15));
		result.Rates.Should().ContainKey(new CurrencyCode("USD")).WhoseValue.Should().Be(1.10m);
		result.Rates.Should().ContainKey(new CurrencyCode("GBP")).WhoseValue.Should().Be(0.85m);
	}

	[Fact]
	public async Task ConvertAsync_WithValidResponse_ShouldParseCorrectly()
	{
		var json = """
		{
			"amount": 100,
			"base": "USD",
			"date": "2024-01-15",
			"rates": {
				"EUR": 85.50
			}
		}
		""";

		var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
		var provider = new FrankfurterCurrencyProvider(httpClient);

		var result = await provider.ConvertAsync(
			100m,
			new CurrencyCode("USD"),
			new CurrencyCode("EUR"),
			CancellationToken.None);

		result.From.Value.Should().Be("USD");
		result.To.Value.Should().Be("EUR");
		result.Amount.Should().Be(100m);
		result.ConvertedAmount.Should().Be(85.50m);
		result.RateUsed.Should().BeApproximately(0.855m, 0.0001m);
		result.AsOf.Should().Be(new DateOnly(2024, 1, 15));
	}

	[Fact]
	public async Task GetHistoricalRatesAsync_WithValidResponse_ShouldParseCorrectly()
	{
		var json = """
		{
			"amount": 1,
			"base": "EUR",
			"start_date": "2024-01-01",
			"end_date": "2024-01-03",
			"rates": {
				"2024-01-01": {
					"USD": 1.10
				},
				"2024-01-02": {
					"USD": 1.11
				},
				"2024-01-03": {
					"USD": 1.12
				}
			}
		}
		""";

		var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
		var provider = new FrankfurterCurrencyProvider(httpClient);

		var range = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 3));
		var result = await provider.GetHistoricalRatesAsync(new CurrencyCode("EUR"), range, CancellationToken.None);

		result.BaseCurrency.Value.Should().Be("EUR");
		result.Range.Start.Should().Be(new DateOnly(2024, 1, 1));
		result.Range.End.Should().Be(new DateOnly(2024, 1, 3));
		result.Points.Should().HaveCount(3);
		result.Points.Should().Contain(p => p.Date == new DateOnly(2024, 1, 1));
		result.Points.Should().Contain(p => p.Date == new DateOnly(2024, 1, 2));
		result.Points.Should().Contain(p => p.Date == new DateOnly(2024, 1, 3));
	}

	[Fact]
	public async Task GetHistoricalRatesAsync_ShouldMapStartDateAndEndDateCorrectly()
	{
		var json = """
		{
			"amount": 1,
			"base": "EUR",
			"start_date": "2024-01-01",
			"end_date": "2024-01-31",
			"rates": {
				"2024-01-01": {
					"USD": 1.10
				}
			}
		}
		""";

		var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
		var provider = new FrankfurterCurrencyProvider(httpClient);

		var range = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31));
		var result = await provider.GetHistoricalRatesAsync(new CurrencyCode("EUR"), range, CancellationToken.None);

		result.Range.Start.Should().Be(new DateOnly(2024, 1, 1));
		result.Range.End.Should().Be(new DateOnly(2024, 1, 31));
	}

	[Fact]
	public async Task ConvertAsync_WhenTargetCurrencyNotInResponse_ShouldThrowInvalidOperationException()
	{
		var json = """
		{
			"amount": 100,
			"base": "USD",
			"date": "2024-01-15",
			"rates": {
				"GBP": 75.00
			}
		}
		""";

		var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
		var provider = new FrankfurterCurrencyProvider(httpClient);

		var act = async () => await provider.ConvertAsync(
			100m,
			new CurrencyCode("USD"),
			new CurrencyCode("EUR"),
			CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("Frankfurter response did not include the requested target currency.");
	}

	private sealed class FakeHttpMessageHandler : HttpMessageHandler
	{
		private readonly HttpStatusCode _statusCode;
		private readonly string _content;

		public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
		{
			_statusCode = statusCode;
			_content = content;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return Task.FromResult(new HttpResponseMessage(_statusCode)
			{
				Content = new StringContent(_content, Encoding.UTF8, "application/json")
			});
		}
	}
}
