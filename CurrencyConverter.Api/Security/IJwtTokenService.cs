using System.Security.Claims;

namespace CurrencyConverter.Api.Security;

public interface IJwtTokenService
{
	string CreateToken(string clientId, IReadOnlyCollection<string> roles);
}