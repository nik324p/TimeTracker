using Microsoft.Extensions.DependencyInjection;

namespace TimeTracker.Core;

/// <summary>DI registration for Core's application services.</summary>
public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the six Core application services as scoped. The caller (the Api) must also register
    /// the dependencies these services consume: the repositories + <see cref="IUnitOfWork"/> +
    /// <see cref="IEventPublisher"/> (Infrastructure), a <see cref="TimeProvider"/>, and the option
    /// instances (<see cref="LatenessOptions"/>, <see cref="HistoryOptions"/>) bound from config.
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<ITapService, TapService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IExclusionService, ExclusionService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        return services;
    }
}
