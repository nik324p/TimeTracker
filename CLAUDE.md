# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Current state: implemented

All five projects are built and wired: `Core`, `Infrastructure`, `Api`, `Tests`, `Demo`. Source lives
under `src/`, tests under `tests/`. The solution is `TimeTracker.slnx` (the new XML solution format —
`dotnet build`/`dotnet test` resolve it directly, no `.sln`). The EF `InitialCreate` migration exists;
`docker-compose.yml` runs the full stack.

Reference doc (still the source of truth for *why*):
- `overview.md` — settled architecture, tooling, and per-project decisions.

**Before changing code:** route paths and JSON field names are normative — keep them verbatim from the
existing endpoints. Honor the contracts in the next section; they are easy to break.

Beyond the 13 spec endpoints, the Api has two **demo-only, env-gated** routes in `Endpoints/DevEndpoints.cs`
(`/dev/create_user`, `/dev/seed_tap`) — there is no spec create-user endpoint and `/card/touch` only stamps
"now", so the Demo needs these to bootstrap users and backfill multi-day history. Both return 404 when
disabled (active only in Development env or with `EnableSeedEndpoints=true`) and reach Core's repository
interfaces directly so the concession stays fenced in the Api, never touching Core's tested surface.

## What this is

`TimeTracker` — a microservice for employee work-time tracking via NFC cards (identification only, not
access control). Workers tap a card twice a day (arrival / departure); admins view history, set schedules,
add schedule exclusions, and read statistics. Deliberately lean **test project**, not production-grade
(especially infrastructure).

## Target architecture (5 projects, one solution)

Strict inward dependency flow — `Core` depends on nothing in the solution:

```
Core  ◄── Infrastructure (implements Core's repo + IEventPublisher interfaces over EF Core / RabbitMQ)
  ▲
  └────── Api (calls Core services, maps Core exceptions to HTTP)
              Tests ──► Core only (mocks Core's interfaces; never references Infrastructure/Api)
              Demo  ──► no project refs; HTTP client of the running Api
```

- **`TimeTracker.Core`** — the unit-tested heart. Rich domain model (entities own their invariants;
  all mutation goes through methods, e.g. `WorkDay.RecordTap` throws `AlreadyTappedException` on a third
  tap). Plain application services (`ITapService`, `ICardService`, `IScheduleService`, `IExclusionService`,
  `IHistoryService`, `IStatisticsService`). Repository + `IUnitOfWork` + `IEventPublisher` *interfaces only*.
  Domain exceptions and POCO Options classes. **Zero infrastructure deps** — no EF, RabbitMQ, ASP.NET, or
  System.Text.Json in entities. *Sole sanctioned exception:* the wire-format `[JsonStringEnumMemberName]`
  attributes on the `ExclusionType`/`StatisticsPeriod` members in `Domain/Enums.cs` (no NuGet dep — STJ ships
  in the shared framework — and it keeps the spec's spaced phrases as enum metadata, deleting two Api
  converters). Lateness/early/under-work derivation lives in the pure static
  `AttendanceEvaluator`, and statistics just aggregate its per-day `DayEvaluation`s (so history and stats
  never disagree).
- **`TimeTracker.Infrastructure`** — implements Core's interfaces. EF Core + Npgsql `AppDbContext`
  (= the Unit of Work; repositories stage changes but never call `SaveChanges`), `IEntityTypeConfiguration<T>`
  Fluent mappings (Core entities stay attribute-free), code-first migrations, and a raw `RabbitMQ.Client` v7
  publisher behind `IEventPublisher`. `AddInfrastructure(IServiceCollection, IConfiguration)` is the single
  DI seam.
- **`TimeTracker.Api`** — public HTTP surface. FastEndpoints (one class per endpoint, REPR). Endpoints stay
  on the happy path: bind DTO → call one Core service → return DTO. They do **not** catch domain exceptions;
  a single `DomainExceptionHandler : IExceptionHandler` maps them to RFC 7807 ProblemDetails.
- **`TimeTracker.Tests`** — xUnit unit tests targeting Core, with mocked repos/publisher and `FakeTimeProvider`.
- **`TimeTracker.Demo`** — Blazor Web App (Interactive Server) that drives the real Api through the full
  scenario in a browser. Server-side HTTP calls only (no CORS, no `.Client` project).

## Non-negotiable contracts (cross-cutting, easy to break)

- **TFM is `net10.0`** for every project. `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in Core.
- **Wire format is snake_case** end-to-end via `JsonNamingPolicy.SnakeCaseLower` (`card_uid`, `user_id`,
  `type_exclusion`, `free_schedule`, …). C# stays PascalCase; the policy does the mapping — don't hand-write
  `[JsonPropertyName]`. The same policy applies to RabbitMQ event payloads and the Demo's client.
- **Route paths and request field names are normative** — copy them verbatim from the existing endpoints
  (`/card/touch`, `/work_time/set`, etc.). All endpoints are `POST`, `application/json`.
- **`user_id` is `long`** (JSON `number`, Postgres `bigint`/identity) in all layers. `card_uid` is a
  case-insensitive `string` (normalize via `CardUidNormalizer`). Surrogate keys are `Guid` (`CreateVersion7`).
  Changing the `user_id` type is a contract change requiring a doc revision.
- **Domain → HTTP exception mapping is fixed** (`DomainExceptionHandler` in the Api): `AlreadyTappedException`
  and `CardAlreadyAssignedException` → 409; `UserNotFoundException`/`CardNotFoundException`/
  `ScheduleNotFoundException` → 404; `InvalidSchedule`/`InvalidExclusion`/`InvalidTap` → 400/422;
  `InvalidLimitException` → 400. Adding a domain exception means adding a mapping row.
- **`type_exclusion`** wire values contain spaces (`"arrive later"`, `"leave earlier"`, `"full working day"`);
  map via `[JsonStringEnumMemberName]` on the enum members (NOT `[EnumMember]` — STJ's built-in
  `JsonStringEnumConverter` ignores that one). The global `JsonStringEnumConverter(SnakeCaseLower)` in
  `Program.cs` honors the attribute; members without it (`week`/`month`/`year`) fall out of the snake_case
  policy. **`filter`** (statistics period) defaults to `month`.
- **Time is always via injected `TimeProvider`** — never `DateTime.Now`/`UtcNow`. Stored instants are UTC;
  calendar-day/lateness comparisons use the local frame. Tests freeze it with `FakeTimeProvider`.
- **Event publishing is fire-and-forget, no outbox** — publish only after `SaveChangesAsync` succeeds;
  log-and-swallow publish failures (the tap still succeeded). Accepted limitation for this project.

## Dependency / build order

`Core` → `Infrastructure` → `Api` → `Demo`; `Tests` references only Core. Each layer references the one(s)
above it, so build top-to-bottom — but `dotnet build`/`dotnet test` on the solution handle ordering for you.

## Commands

```bash
# Build / test the whole solution (TimeTracker.slnx)
dotnet build
dotnet test                                   # runs TimeTracker.Tests against Core

# Run a single test
dotnet test --filter "FullyQualifiedName~TapServiceTests"
dotnet test --filter "Name=Tap_SecondArrivalSameDay_Throws"

# EF Core migrations — dotnet-ef is pinned in dotnet-tools.json (local tool, not global)
dotnet tool restore                           # one-time, installs the pinned dotnet-ef
dotnet ef migrations add <Name> -p src/TimeTracker.Infrastructure -s src/TimeTracker.Api -o Migrations
dotnet ef migrations script    -p src/TimeTracker.Infrastructure -s src/TimeTracker.Api   # review SQL
# Migration lives in Infrastructure; Api is the startup host for config/DI. Migrations are also applied
# at startup via Database.MigrateAsync() (gated by RunMigrationsAtStartup).

# Full stack: postgres + rabbitmq + api + one-shot tests container + long-running demo
docker compose up --build
# Api/Swagger: http://localhost:8080/swagger   health: /health
# Demo:        http://localhost:8090
# Note: a root .dockerignore is required — it keeps host bin/obj out of the build context,
# otherwise `compose build api` fails publish with NETSDK1064.
```

## Settled tooling choices (don't re-litigate)

FastEndpoints (REPR) with **.NET 10 built-in DataAnnotations validation**, opting out of FE's
FluentValidation. EF Core + Npgsql (code-first, snake_case columns). Raw `RabbitMQ.Client` v7 (topic
exchange, publish-only). xUnit + **AwesomeAssertions** (MIT fork of FluentAssertions v7) + NSubstitute +
`FakeTimeProvider`. Test Data Builders, AAA, `Method_Scenario_Expected` naming. Deliberate upgrade paths
(Wolverine, NodaTime, Vogen, MassTransit, EF migration bundles) are *future* options, not the current scope.
