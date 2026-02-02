namespace CurrencyConverter.Domain.Paging;

public readonly record struct PageRequest(int PageNumber, int PageSize)
{
	public static PageRequest Create(int pageNumber, int pageSize)
	{
		if (pageNumber <= 0) throw new ArgumentOutOfRangeException(nameof(pageNumber));
		if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

		return new PageRequest(pageNumber, pageSize);
	}

	public int Skip() => (PageNumber - 1) * PageSize;
}