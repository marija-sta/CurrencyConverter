using Asp.Versioning;
using CurrencyConverter.Api.DependencyInjection;
using CurrencyConverter.Api.Middleware;
using CurrencyConverter.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ExceptionHandlingMiddleware>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

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
    app.MapScalarApiReference(options =>
    {
        options.Title = "CurrencyConverter API";
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
