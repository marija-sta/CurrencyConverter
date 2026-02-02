using System.Text.Json;
using CurrencyConverter.Infrastructure.Providers.Responses;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Infrastructure.Providers.Responses;

public sealed class FrankfurterHistoricalResponseTests
{
	[Fact]
	public void Deserialize_WithValidJson_ShouldMapAllProperties()
	{
		var json = """
		{
			"amount": 1,
			"base": "EUR",
			"start_date": "2024-01-01",
			"end_date": "2024-01-03",
			"rates": {
				"2024-01-01": {
					"USD": 1.10,
					"GBP": 0.85
				},
				"2024-01-02": {
					"USD": 1.11,
					"GBP": 0.86
				},
				"2024-01-03": {
					"USD": 1.12,
					"GBP": 0.87
				}
			}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterHistoricalResponse>(json);

		response.Should().NotBeNull();
		response!.Amount.Should().Be(1);
		response.Base.Should().Be("EUR");
		response.StartDate.Should().Be("2024-01-01");
		response.EndDate.Should().Be("2024-01-03");
		response.Rates.Should().HaveCount(3);
		response.Rates["2024-01-01"]["USD"].Should().Be(1.10m);
		response.Rates["2024-01-02"]["USD"].Should().Be(1.11m);
		response.Rates["2024-01-03"]["USD"].Should().Be(1.12m);
	}

	[Fact]
	public void Deserialize_WithStartDateEndDate_ShouldMapJsonPropertyNames()
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

		var response = JsonSerializer.Deserialize<FrankfurterHistoricalResponse>(json);

		response.Should().NotBeNull();
		response!.StartDate.Should().Be("2024-01-01");
		response.EndDate.Should().Be("2024-01-31");
	}

	[Fact]
	public void Deserialize_WithMultipleCurrenciesPerDate_ShouldMapAll()
	{
		var json = """
		{
			"amount": 100,
			"base": "USD",
			"start_date": "2024-01-01",
			"end_date": "2024-01-01",
			"rates": {
				"2024-01-01": {
					"EUR": 85.50,
					"GBP": 75.20,
					"JPY": 12500.00,
					"CHF": 90.15
				}
			}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterHistoricalResponse>(json);

		response.Should().NotBeNull();
		response!.Rates["2024-01-01"].Should().HaveCount(4);
		response.Rates["2024-01-01"]["EUR"].Should().Be(85.50m);
		response.Rates["2024-01-01"]["GBP"].Should().Be(75.20m);
		response.Rates["2024-01-01"]["JPY"].Should().Be(12500.00m);
		response.Rates["2024-01-01"]["CHF"].Should().Be(90.15m);
	}

	[Fact]
	public void Deserialize_WithEmptyRates_ShouldReturnEmptyDictionary()
	{
		var json = """
		{
			"amount": 1,
			"base": "EUR",
			"start_date": "2024-01-01",
			"end_date": "2024-01-31",
			"rates": {}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterHistoricalResponse>(json);

		response.Should().NotBeNull();
		response!.Rates.Should().BeEmpty();
	}

	[Fact]
	public void Deserialize_WithSingleDate_ShouldWork()
	{
		var json = """
		{
			"amount": 1,
			"base": "EUR",
			"start_date": "2024-01-15",
			"end_date": "2024-01-15",
			"rates": {
				"2024-01-15": {
					"USD": 1.10
				}
			}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterHistoricalResponse>(json);

		response.Should().NotBeNull();
		response!.StartDate.Should().Be(response.EndDate);
		response.Rates.Should().ContainKey("2024-01-15");
	}

	[Fact]
	public void Deserialize_WithDecimalPrecision_ShouldPreserveAccuracy()
	{
		var json = """
		{
			"amount": 1.5,
			"base": "EUR",
			"start_date": "2024-01-01",
			"end_date": "2024-01-01",
			"rates": {
				"2024-01-01": {
					"USD": 1.123456789
				}
			}
		}
		""";

		var response = JsonSerializer.Deserialize<FrankfurterHistoricalResponse>(json);

		response.Should().NotBeNull();
		response!.Amount.Should().Be(1.5m);
		response.Rates["2024-01-01"]["USD"].Should().Be(1.123456789m);
	}
}
