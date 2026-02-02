using CurrencyConverter.Domain.Paging;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain.Paging;

public sealed class PagedResultTests
{
	[Fact]
	public void Constructor_WithValidData_ShouldSetProperties()
	{
		var items = new List<string> { "item1", "item2", "item3" };
		var result = new PagedResult<string>(items, 1, 10, 30);

		result.Items.Should().HaveCount(3);
		result.PageNumber.Should().Be(1);
		result.PageSize.Should().Be(10);
		result.TotalItems.Should().Be(30);
	}

	[Theory]
	[InlineData(30, 10, 3)]
	[InlineData(25, 10, 3)]
	[InlineData(31, 10, 4)]
	[InlineData(0, 10, 0)]
	[InlineData(10, 10, 1)]
	public void TotalPages_ShouldCalculateCorrectly(int totalItems, int pageSize, int expectedTotalPages)
	{
		var result = new PagedResult<string>(new List<string>(), 1, pageSize, totalItems);

		result.TotalPages.Should().Be(expectedTotalPages);
	}

	[Fact]
	public void TotalPages_WithZeroTotalItems_ShouldReturnZero()
	{
		var result = new PagedResult<string>(new List<string>(), 1, 10, 0);

		result.TotalPages.Should().Be(0);
	}
}
