using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeTracker.Demo;
using TimeTracker.Demo.Components;

var builder = WebApplication.CreateBuilder(args);

// 1. Blazor Web App — Interactive Server only (SignalR circuit; no WebAssembly/.Client project).
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. API base URL from config/env (compose injects http://api:8080). Fail fast if missing.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

// 3. One shared JSON options instance speaking the API's wire format (snake_case end-to-end).
//    The enum converter makes DayOfWeek round-trip as "monday".."sunday" (the API's `days` encoding).
//    type_exclusion / filter are modeled as plain strings in the contracts, so they need no converter.
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
};
builder.Services.AddSingleton(jsonOptions);

// 4. Typed HttpClient via IHttpClientFactory, base address = ApiBaseUrl.
builder.Services.AddHttpClient<TimeTrackerApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/problem+json"));
});

// 5. One request log per circuit, shared across both tabs (survives nav between tabs).
builder.Services.AddScoped<RequestLog>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
