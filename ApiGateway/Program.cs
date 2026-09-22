using ApiGateway.Authentication;
using ApiGateway.Authorization;
using ApiGateway.Endpoints;
using ApiGateway.Options;
using ApiGateway.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Shared.Kernel.AspNetCore.OpenApi;
using Shared.Kernel.Extensions;
using Shared.Kernel.Redis;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
builder.Services.ConfigureOption<JwtValidationSettings>(configuration);
builder.Services.ConfigureOption<DocsAccessSettings>(configuration);
builder.Services.ConfigureOption<RateLimitingSettings>(configuration);

builder.Services.AddRedisAccessTokenBlacklist(configuration);
builder.Services.AddRedisRateLimiter(configuration);
builder.Services.AddRedisUserAccessRevocation(configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(configuration.GetSection("ReverseProxy"));

builder.Services.AddGatewayJwtBearerAuthentication();
builder.Services.AddDocsAccessAuthorization();

builder.Services.AddOpenApiWithBearerAuth(schemeName: JwtBearerDefaults.AuthenticationScheme);

var app = builder.Build();

await DocsAllowlistSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapScalarDocumentation();
}

app.UseRouting();

app.UseMiddleware<LoginRateLimitingMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapDocsAccessEndpoints();

app.MapReverseProxy();

app.Run();