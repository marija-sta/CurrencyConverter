using System.Diagnostics.CodeAnalysis;

namespace CurrencyConverter.Api.Security;

[ExcludeFromCodeCoverage]
public sealed record DevTokenRequest(string ClientId, IEnumerable<string>? Roles);