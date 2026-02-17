using FiapCloudGames.Payments.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddApplicationServices(configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiConfiguration();
builder.Services.AddLoggingConfiguration();
builder.Services.AddHealthCheckConfiguration(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApiConfiguration();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthCheckEndpoints();

app.Run();