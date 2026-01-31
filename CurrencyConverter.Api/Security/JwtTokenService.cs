using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverter.Api.Security;

public sealed class JwtTokenService : IJwtTokenService
{
	private readonly JwtOptions options;
	private readonly TimeProvider timeProvider;

	public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
	{
		this.options = options.Value;
		this.timeProvider = timeProvider;
	}

	public string CreateToken(string clientId, IReadOnlyCollection<string> roles)
	{
		var now = timeProvider.GetUtcNow();

		var claims = new List<Claim>
		{
			new("sub", clientId)
		};

		foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r))
								.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: options.Issuer,
			audience: options.Audience,
			claims: claims,
			notBefore: now.UtcDateTime,
			expires: now.AddMinutes(options.TokenLifetimeMinutes)
						.UtcDateTime,
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}