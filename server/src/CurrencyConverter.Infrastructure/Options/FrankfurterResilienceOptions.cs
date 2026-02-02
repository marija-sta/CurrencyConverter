namespace CurrencyConverter.Infrastructure.Options;

public sealed class FrankfurterResilienceOptions
{
	public const string SectionName = "FrankfurterResilience";
	public int TimeoutSeconds { get; init; } = 10;

	public int RetryMaxAttempts { get; init; } = 3;
	public int RetryBaseDelayMilliseconds { get; init; } = 200;

	public int CircuitBreakerSamplingSeconds { get; init; } = 30;
	public int CircuitBreakerMinimumThroughput { get; init; } = 10;
	public double CircuitBreakerFailureRatio { get; init; } = 0.5;
	public int CircuitBreakerBreakSeconds { get; init; } = 20;
}