using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CurrencyConverter.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverter.Api.DependencyInjection;

public static class ApiSecurityServiceCollectionExtensions
{
	public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
		services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

		services.AddSingleton(TimeProvider.System);
		services.AddSingleton<IJwtTokenService, JwtTokenService>();

		var jwt = configuration.GetSection(JwtOptions.SectionName)
								.Get<JwtOptions>() ?? new JwtOptions();

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidIssuer = jwt.Issuer,

						ValidateAudience = true,
						ValidAudience = jwt.Audience,

						ValidateIssuerSigningKey = true,
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

						ValidateLifetime = true,
						ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),

						RoleClaimType = ClaimTypes.Role
					};
				});

		services.AddAuthorization(options =>
		{
			options.FallbackPolicy = new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build();

			options.AddPolicy("RatesRead", policy => policy.RequireRole("rates.read", "admin"));
			options.AddPolicy("Convert", policy => policy.RequireRole("convert", "admin"));
			options.AddPolicy("HistoryRead", policy => policy.RequireRole("history.read", "admin"));
		});

		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

			var rate = configuration.GetSection(RateLimitingOptions.SectionName)
									.Get<RateLimitingOptions>()
						?? new RateLimitingOptions();

			options.AddPolicy("ApiPolicy", httpContext =>
			{
				var clientId = httpContext.User.FindFirstValue("sub");
				var partitionKey = !string.IsNullOrWhiteSpace(clientId)
					? $"client:{clientId}"
					: $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

				return RateLimitPartition.GetTokenBucketLimiter(
					partitionKey,
					_ => new TokenBucketRateLimiterOptions
					{
						TokenLimit = rate.TokenBucket.PermitLimit,
						QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
						QueueLimit = rate.TokenBucket.QueueLimit,
						ReplenishmentPeriod = TimeSpan.FromSeconds(rate.TokenBucket.ReplenishmentPeriodSeconds),
						TokensPerPeriod = rate.TokenBucket.TokensPerPeriod,
						AutoReplenishment = true
					});
			});
		});

		return services;
	}
}