using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TimeTracker.Infrastructure;

/// <summary>Startup migration helper, invoked by the Api host after the app is built.</summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Applies all pending EF migrations (creates the database if absent), unless the
    /// <c>RunMigrationsAtStartup</c> config flag is set to <c>false</c> (default <c>true</c>).
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("RunMigrationsAtStartup", defaultValue: true))
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}