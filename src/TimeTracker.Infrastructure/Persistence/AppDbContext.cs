using Microsoft.EntityFrameworkCore;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

/// <summary>
/// The EF Core context and the Unit of Work: <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
/// commits one atomic transaction. Repositories stage changes; only the caller commits.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Card> Cards => Set<Card>();

    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();

    public DbSet<ScheduleExclusion> ScheduleExclusions => Set<ScheduleExclusion>();

    public DbSet<WorkDay> WorkDays => Set<WorkDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Discover every IEntityTypeConfiguration<T> in this assembly (one per entity).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }
}
