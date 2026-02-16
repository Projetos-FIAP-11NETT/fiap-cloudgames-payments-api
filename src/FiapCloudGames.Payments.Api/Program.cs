using FiapCloudGames.Payments.Application.Interfaces;
using FiapCloudGames.Payments.Infrastructure.Data;
using FiapCloudGames.Payments.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("FiapCloudGames.Payments.Infrastructure")
    ));

// Repositories
builder.Services.AddScoped<IPaymentsRepository, PaymentsRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();