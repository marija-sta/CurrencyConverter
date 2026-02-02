using CurrencyConverter.Application.Abstractions.Providers;
using CurrencyConverter.Infrastructure.Options;
using CurrencyConverter.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CurrencyConverter.UnitTests.Infrastructure.Providers;

public sealed class CurrencyProviderFactoryTests
{
	[Fact]
	public void GetProvider_WithFrankfurterKey_ShouldReturnFrankfurterProvider()
	{
		var services = new ServiceCollection();
		var provider = Substitute.For<ICurrencyProvider>();

		services.AddKeyedSingleton(CurrencyProviderKey.Frankfurter, provider);

		var serviceProvider = services.BuildServiceProvider();
		var options = Options.Create(new ProviderOptions { ActiveProvider = CurrencyProviderKey.Frankfurter });
		var factory = new CurrencyProviderFactory(serviceProvider, options);

		var result = factory.GetProvider();

		result.Should()
			.BeSameAs(provider);
	}

	[Fact]
	public void GetProvider_ShouldUseActiveProviderFromOptions()
	{
		var provider = Substitute.For<ICurrencyProvider>();
		var services = new ServiceCollection();

		services.AddKeyedSingleton(CurrencyProviderKey.Frankfurter, provider);

		var serviceProvider = services.BuildServiceProvider();
		var options = Options.Create(new ProviderOptions { ActiveProvider = CurrencyProviderKey.Frankfurter });
		var factory = new CurrencyProviderFactory(serviceProvider, options);

		var result = factory.GetProvider();

		result.Should()
			.NotBeNull();
		result.Should()
			.BeSameAs(provider);
	}
}
