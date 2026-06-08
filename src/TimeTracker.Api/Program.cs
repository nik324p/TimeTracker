using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Options;
using TimeTracker.Api;
using TimeTracker.Core;
using TimeTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Time (deterministic-friendly; Core/services never call DateTime.Now directly) ---
builder.Services.AddSingleton(TimeProvider.System);

// --- Persistence + messaging adapters (DbContext, repositories, RabbitMQ publisher, options) ---
builder.Services.AddInfrastructure(builder.Configuration);

// --- Core application services ---
builder.Services.AddCore();

// Core services consume the raw option POCOs (not IOptions<T>), so bind then expose .Value.
builder.Services.Configure<LatenessOptions>(builder.Configuration.GetSection(LatenessOptions.SectionName));
builder.Services.Configure<HistoryOptions>(builder.Configuration.GetSection(HistoryOptions.SectionName));
builder.Services.Configure<WorkScheduleOptions>(builder.Configuration.GetSection(WorkScheduleOptions.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<LatenessOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<HistoryOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<WorkScheduleOptions>>().Value);

// --- HTTP surface ---
builder.Services.AddFastEndpoints();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "TimeTracker API";
        s.Version = "v1";
    };
});
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply pending EF migrations at startup (gated by the RunMigrationsAtStartup config flag).
await app.Services.MigrateDatabaseAsync();

// Global domain-exception → ProblemDetails handler sits at the top of the pipeline.
app.UseExceptionHandler();

app.UseFastEndpoints(c =>
{
    // RFC 7807 ProblemDetails for validation/error responses.
    c.Errors.UseProblemDetails();

    // snake_case wire format (card_uid, user_id, type_exclusion, …) everywhere.
    c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    c.Serializer.Options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    c.Serializer.Options.PropertyNameCaseInsensitive = true;
    // Enums serialize as snake_case strings; spec phrases with spaces ("arrive later", "entire period")
    // come from [JsonStringEnumMemberName] on the Core enum members, which this converter honors.
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

    // DataAnnotations validation for every request DTO (no FE FluentValidation validators registered).
    c.Endpoints.Configurator = ep => ep.PreProcessor<DataAnnotationsValidator>(Order.Before);
});

app.UseSwaggerGen();
app.MapHealthChecks("/health");

app.Run();

// Exposes the entry point for WebApplicationFactory-based integration tests.
public partial class Program;
