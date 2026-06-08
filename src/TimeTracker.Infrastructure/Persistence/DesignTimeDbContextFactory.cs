using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeTracker.Infrastructure;

/// <summary>
/// Lets <c>dotnet ef</c> build an <see cref="AppDbContext"/> at design time without the Api host.
/// The connection string is only used to scaffold/inspect migrations (no live DB needed for
/// <c>migrations add</c>); override it via the <c>TIMETRACKER_DESIGN_CONNECTION</c> env var.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TIMETRACKER_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=timetracker;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
