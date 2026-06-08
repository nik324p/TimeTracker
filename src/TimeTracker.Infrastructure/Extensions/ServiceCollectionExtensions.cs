using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

/// <summary>The single composition seam for the persistence + messaging adapters.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Wires the DbContext (Unit of Work) + repositories (scoped) + RabbitMQ publisher (singleton) +
    /// options. The Api needs no EF/RabbitMQ knowledge beyond this call. It must still register a
    /// <see cref="TimeProvider"/> and call <c>AddCore()</c> for the Core services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PostgresOptions>(configuration.GetSection(PostgresOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var postgres = configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>()
            ?? throw new InvalidOperationException(
                $"Missing required configuration section '{PostgresOptions.SectionName}'.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(postgres.ConnectionString)
            .UseSnakeCaseNamingConvention());

        // The DbContext is the Unit of Work; repositories share its scope.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
        services.AddScoped<IScheduleExclusionRepository, ScheduleExclusionRepository>();
        services.AddScoped<IWorkDayRepository, WorkDayRepository>();

        // Long-lived AMQP connection ⇒ singleton. Never captures a scoped dependency.
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }
}