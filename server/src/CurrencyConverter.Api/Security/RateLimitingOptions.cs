namespace CurrencyConverter.Api.Security;

public sealed class RateLimitingOptions
{
	public const string SectionName = "RateLimiting";

	public TokenBucketOptions TokenBucket { get; init; } = new();

	public sealed class TokenBucketOptions
	{
		public int PermitLimit { get; init; } = 60;

		public int QueueLimit { get; init; } = 0;

		public int TokensPerPeriod { get; init; } = 60;

		public int ReplenishmentPeriodSeconds { get; init; } = 60;
	}
}