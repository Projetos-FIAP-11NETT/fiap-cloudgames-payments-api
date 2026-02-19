using FiapCloudGames.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Payments.Api.Configuration;

public static class MigrationConfig
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        var scope = app.ApplicationServices.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        dataContext.Database.Migrate();
    }
}