using FiapCloudGames.Payments.Api.Configuration;
using FiapCloudGames.Payments.Api.Middleware;
using FiapCloudGames.Payments.Observability.Configurations;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddApplicationServices(configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiConfiguration();
builder.Services.AddLoggingConfiguration();
builder.Services.AddHealthCheckConfiguration(configuration);
builder.Services.AddObservabilityConfig();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<FiapCloudGames.Payments.Observability.Middleware.ObservabilityMiddleware>();
app.ApplyMigrations();
app.MapControllers();

// if (app.Environment.IsDevelopment())
// {
    app.MapOpenApiConfiguration();
// }

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapHealthCheckEndpoints();

app.Run();