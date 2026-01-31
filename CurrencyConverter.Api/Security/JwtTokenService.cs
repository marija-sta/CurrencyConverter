using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverter.Api.Security;

public sealed class JwtTokenService : IJwtTokenService
{
	private readonly JwtOptions _options;
	private readonly TimeProvider _timeProvider;

	public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
	{
		this._options = options.Value;
		this._timeProvider = timeProvider;
	}

	public string CreateToken(string clientId, IReadOnlyCollection<string> roles)
	{
		var now = this._timeProvider.GetUtcNow();

		var claims = new List<Claim>
		{
			new("sub", clientId)
		};

		foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r))
								.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			claims.Add(new Claim(ClaimTypes.Role, role));
		}

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._options.SigningKey));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: this._options.Issuer,
			audience: this._options.Audience,
			claims: claims,
			notBefore: now.UtcDateTime,
			expires: now.AddMinutes(this._options.TokenLifetimeMinutes)
						.UtcDateTime,
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}