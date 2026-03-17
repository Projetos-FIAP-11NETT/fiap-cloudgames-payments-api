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

        logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | 🔄 Aplicando migrações do banco de dados...", "n/a");

        try
        {
            dataContext.Database.Migrate();
            logger.LogInformation("[payments-service] CorrelationId: {CorrelationId} | ✅ Migrações aplicadas com sucesso", "n/a");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[payments-service] CorrelationId: {CorrelationId} | ❌ Falha ao aplicar migrações", "n/a");
            throw;
        }
    }
}