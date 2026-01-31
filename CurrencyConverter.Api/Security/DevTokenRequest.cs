namespace CurrencyConverter.Api.Security;

public sealed record DevTokenRequest(string ClientId, IEnumerable<string>? Roles);