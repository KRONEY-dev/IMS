using InventoryService.Application;
using InventoryService.Infrastructure;
using Prometheus;
using Scalar.AspNetCore;
using Shared.Kernel.AspNetCore.HealthChecks;
using Shared.Kernel.AspNetCore.OpenApi;
using Shared.Kernel.AspNetCore.Requests;
using Shared.Kernel.AspNetCore.Requests.Authentication;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
builder.Services.InjectApplication(configuration);
builder.Services.InjectInfrastructure(configuration);

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

builder.Services.AddJwtClaimsAuthentication();

builder.Services.AddAuthorization();

builder.Services.AddOpenApiWithBearerAuth(configuration["OpenApi:GatewayBasePath"]);

var app = builder.Build();

app.UseHttpMetrics();

app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();

app.UseRequestContext();

app.UseAuthorization();

app.MapControllers();
app.MapInventoryHub();
app.MapLivenessAndReadinessHealthChecks();
app.MapMetrics();

app.Run();

// Exposes the top-level-statements entry point to WebApplicationFactory<Program> in tests.
public partial class Program
{
}