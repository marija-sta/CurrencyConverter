using System.Diagnostics.CodeAnalysis;
using CurrencyConverter.Api.Security;

namespace CurrencyConverter.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class DevAuthEndpoints
{
	public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder endpoints)
	{
		// Development-only helper endpoint used to generate JWTs for local testing of auth, RBAC and rate limiting.
		// This is not a real authentication flow and must never be enabled outside Development.
		endpoints.MapPost("/api/dev/token", (IJwtTokenService tokenService, DevTokenRequest request) =>
		{
			if (string.IsNullOrWhiteSpace(request.ClientId))
			{
				return Results.BadRequest(new { message = "ClientId is required." });
			}

			var roles = request.Roles?.Where(r => !string.IsNullOrWhiteSpace(r))
								.ToArray() ?? [];

			var token = tokenService.CreateToken(request.ClientId, roles);
			return Results.Ok(new { token });
		})
		.AllowAnonymous()
		.DisableRateLimiting();

		return endpoints;
	}
}