using Asp.Versioning;
using CurrencyConverter.Api.DependencyInjection;
using CurrencyConverter.Api.Endpoints;
using CurrencyConverter.Api.Middleware;
using CurrencyConverter.Application.DependencyInjection;
using CurrencyConverter.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseApiSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
			  .AllowAnyHeader()
			  .AllowAnyMethod()
			  .AllowCredentials()
			  .WithExposedHeaders("X-Correlation-ID");
	});
});

builder.Services.AddSingleton<ExceptionHandlingMiddleware>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddApiObservability(builder.Configuration);

builder.Services
       .AddApiVersioning(options =>
       {
           options.DefaultApiVersion = new ApiVersion(1);
           options.AssumeDefaultVersionWhenUnspecified = true;
           options.ReportApiVersions = true;
       })
       .AddMvc()
       .AddApiExplorer(options =>
       {
           options.GroupNameFormat = "'v'V";
           options.SubstituteApiVersionInUrl = true;
       });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => { options.Title = "CurrencyConverter API"; });

    // Development-only token endpoint must be mapped separately to allow anonymous access
    app.MapDevAuthEndpoints();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();

app.UseMiddleware<RequestLogContextMiddleware>();

app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapControllers()
   .RequireRateLimiting("ApiPolicy")
   .RequireAuthorization();

app.Run();

public partial class Program
{
}
