using FiapCloudGames.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Payments.Api.Configuration;

public static class MigrationConfig
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        var scope = app.ApplicationServices.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationConfig");

        logger.LogInformation("🔄 Aplicando migrações do banco de dados...");

        try
        {
            dataContext.Database.Migrate();
            logger.LogInformation("✅ Migrações aplicadas com sucesso");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Falha ao aplicar migrações");
            throw;
        }
    }
}