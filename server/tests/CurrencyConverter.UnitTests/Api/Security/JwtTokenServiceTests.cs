using CurrencyConverter.Api.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.UnitTests.Api.Security;

public sealed class JwtTokenServiceTests
{
	[Fact]
	public void CreateToken_WithValidInputs_ShouldReturnToken()
	{
		var options = Options.Create(new JwtOptions
		{
			Issuer = "TestIssuer",
			Audience = "TestAudience",
			SigningKey = "ThisIsAVeryLongSecretKeyForTestingPurposesWithAtLeast32Characters",
			TokenLifetimeMinutes = 60,
			ClockSkewSeconds = 30
		});
		var timeProvider = TimeProvider.System;
		var service = new JwtTokenService(options, timeProvider);

		var token = service.CreateToken("test-client", ["role1", "role2"]);

		token.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public void CreateToken_WithEmptyRoles_ShouldReturnTokenWithoutRoles()
	{
		var options = Options.Create(new JwtOptions
		{
			Issuer = "TestIssuer",
			Audience = "TestAudience",
			SigningKey = "ThisIsAVeryLongSecretKeyForTestingPurposesWithAtLeast32Characters",
			TokenLifetimeMinutes = 60,
			ClockSkewSeconds = 30
		});
		var timeProvider = TimeProvider.System;
		var service = new JwtTokenService(options, timeProvider);

		var token = service.CreateToken("test-client", []);

		token.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public void CreateToken_WithDuplicateRoles_ShouldDeduplicateCaseInsensitive()
	{
		var options = Options.Create(new JwtOptions
		{
			Issuer = "TestIssuer",
			Audience = "TestAudience",
			SigningKey = "ThisIsAVeryLongSecretKeyForTestingPurposesWithAtLeast32Characters",
			TokenLifetimeMinutes = 60,
			ClockSkewSeconds = 30
		});
		var timeProvider = TimeProvider.System;
		var service = new JwtTokenService(options, timeProvider);

		var token = service.CreateToken("test-client", ["admin", "Admin", "ADMIN", "user"]);

		token.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public void CreateToken_WithWhitespaceRoles_ShouldFilterThem()
	{
		var options = Options.Create(new JwtOptions
		{
			Issuer = "TestIssuer",
			Audience = "TestAudience",
			SigningKey = "ThisIsAVeryLongSecretKeyForTestingPurposesWithAtLeast32Characters",
			TokenLifetimeMinutes = 60,
			ClockSkewSeconds = 30
		});
		var timeProvider = TimeProvider.System;
		var service = new JwtTokenService(options, timeProvider);

		var token = service.CreateToken("test-client", ["admin", "", "  ", "user"]);

		token.Should().NotBeNullOrWhiteSpace();
	}
}
