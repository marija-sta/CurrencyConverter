using CurrencyConverter.Domain.Paging;
using FluentAssertions;

namespace CurrencyConverter.UnitTests.Domain.Paging;

public sealed class PageRequestTests
{
	[Fact]
	public void Create_WithValidParameters_ShouldReturnPageRequest()
	{
		var pageRequest = PageRequest.Create(1, 30);

		pageRequest.PageNumber.Should().Be(1);
		pageRequest.PageSize.Should().Be(30);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public void Create_WithInvalidPageNumber_ShouldThrowArgumentOutOfRangeException(int pageNumber)
	{
		var act = () => PageRequest.Create(pageNumber, 30);

		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithParameterName("pageNumber");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public void Create_WithInvalidPageSize_ShouldThrowArgumentOutOfRangeException(int pageSize)
	{
		var act = () => PageRequest.Create(1, pageSize);

		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithParameterName("pageSize");
	}

	[Theory]
	[InlineData(1, 30, 0)]
	[InlineData(2, 30, 30)]
	[InlineData(3, 20, 40)]
	[InlineData(5, 10, 40)]
	public void Skip_ShouldReturnCorrectOffset(int pageNumber, int pageSize, int expectedSkip)
	{
		var pageRequest = new PageRequest(pageNumber, pageSize);

		pageRequest.Skip().Should().Be(expectedSkip);
	}
}
