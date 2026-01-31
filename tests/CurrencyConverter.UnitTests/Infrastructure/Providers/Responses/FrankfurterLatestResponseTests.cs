using System.Text.Json;
using CurrencyConverter.Infrastructure.Providers.Responses;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Infrastructure.Providers.Responses;

public sealed class FrankfurterLatestResponseTests
{
	[Fact]
	public void Deserialize_WithValidJson_ShouldMapAllProperties()
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

		var response = JsonSerializer.Deserialize<FrankfurterLatestResponse>(json);

		response.Should().NotBeNull();
		response!.Amount.Should().Be(1);
		response.Base.Should().Be("EUR");
		response.Date.Should().Be("2024-01-15");
		response.Rates.Should().HaveCount(2);
		response.Rates["USD"].Should().Be(1.10m);
		response.Rates["GBP"].Should().Be(0.85m);
	}

	[Fact]
	public void Deserialize_WithEmptyRates_ShouldReturnEmptyDictionary()
	{
		var json = """
		{
			"amount": 1,
			"base": "EUR",
			"date": "2024-01-15",
			"rates": {}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterLatestResponse>(json);

		response.Should().NotBeNull();
		response!.Rates.Should().BeEmpty();
	}

	[Fact]
	public void Deserialize_WithMultipleCurrencies_ShouldMapAll()
	{
		var json = """
		{
			"amount": 100,
			"base": "USD",
			"date": "2024-01-15",
			"rates": {
				"EUR": 85.50,
				"GBP": 75.20,
				"JPY": 12500.00,
				"CHF": 90.15
			}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterLatestResponse>(json);

		response.Should().NotBeNull();
		response!.Amount.Should().Be(100);
		response.Base.Should().Be("USD");
		response.Rates.Should().HaveCount(4);
		response.Rates["EUR"].Should().Be(85.50m);
		response.Rates["GBP"].Should().Be(75.20m);
		response.Rates["JPY"].Should().Be(12500.00m);
		response.Rates["CHF"].Should().Be(90.15m);
	}

	[Fact]
	public void Deserialize_WithDecimalPrecision_ShouldPreserveAccuracy()
	{
		var json = """
		{
			"amount": 1.5,
			"base": "EUR",
			"date": "2024-01-15",
			"rates": {
				"USD": 1.123456789
			}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterLatestResponse>(json);

		response.Should().NotBeNull();
		response!.Amount.Should().Be(1.5m);
		response.Rates["USD"].Should().Be(1.123456789m);
	}
}
