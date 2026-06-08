# TimeTracker — Project Overview

## Context
A microservice for employee work-time tracking via NFC cards (identification only, not
access control). Employees tap twice a day (arrival / departure). Admins view history,
set work schedules, and add schedule exclusions. The full functional spec lives in
`requirements.md`. This document captures the agreed architecture and tooling so we have
a stable foundation before writing code.

It is a **test project** — deliberately lean, not production-grade (especially on the
infrastructure side).

## Solution shape
Four .NET projects, with `docker-compose.yml` as the single orchestrator.

| Project | Responsibility |
|---|---|
| `TimeTracker.Api` | HTTP endpoints, request/response DTOs, input validation, DI wiring, Swagger |
| `TimeTracker.Core` | domain models + business logic + interfaces — the unit-tested heart; no external deps |
| `TimeTracker.Infrastructure` | EF Core `DbContext` + migrations + repository impls + RabbitMQ publisher impl |
| `TimeTracker.Tests` | xUnit unit tests targeting `Core` |

## Tooling
- **TFM:** `net10.0` (only SDK installed locally; satisfies the ".NET 8+" requirement)
- **Data access:** EF Core + Npgsql (code-first migrations)
- **JSON:** System.Text.Json with `SnakeCaseLower` naming policy (matches `card_uid`,
  `user_id`, `type_exclusion` from the spec)
- **Validation:** built-in Minimal API validation (.NET 10, source-generated from
  DataAnnotations on request DTOs) — see *Architectural decisions › TimeTracker.Api*
- **Docs:** Swagger / OpenAPI (Swashbuckle) + `README.md`
- **Tests:** xUnit + AwesomeAssertions (free FluentAssertions v7 fork) + NSubstitute
  (mock repositories / publisher) — see *Architectural decisions › TimeTracker.Tests*
- **Config:** `appsettings.json` + Options classes — business-logic config (lateness
  grace minutes, default work hours, RabbitMQ exchange name) and connection config
  (Postgres / RabbitMQ)
- **Containers:** multi-stage `Dockerfile`; `docker-compose.yml` runs postgres +
  rabbitmq + api + a one-shot tests container

## Architectural decisions
Per-project choices for architecture, patterns, and libraries — built up project by
project. Each entry records the picked approach and why it fits this lean test project.

### TimeTracker.Api
- **Endpoint style — FastEndpoints (REPR pattern).** One class per endpoint
  (`Endpoint<TRequest, TResponse>`): strongly typed, self-contained routing, versioning,
  and OpenAPI metadata. Keeps each endpoint isolated as the surface grows (attendance
  tap, history, schedules, exclusions).
- **Error → HTTP — global `IExceptionHandler` + ProblemDetails.** Core throws domain
  exceptions (`AlreadyTappedException`, `UserNotFoundException`, …); a single
  `DomainExceptionHandler` maps them to RFC 7807 ProblemDetails responses. Endpoints stay
  on the happy path with no per-handler error plumbing.
- **Validation — built-in Minimal API validation (.NET 10).** Source-generated from
  DataAnnotations attributes on request DTOs; emits a 400 ProblemDetails automatically.
  No third-party dependency, AOT-friendly.
  - *Note:* this supersedes the FluentValidation line under **Tooling**, and FastEndpoints
    ships its own FluentValidation-based validation — we'll opt out of FE validation in
    favor of the .NET 10 built-in path (integration detail to confirm at implementation).

### TimeTracker.Core
- **Domain model — rich model.** Entities own their data *and* the invariants that
  protect it; state changes go through methods (e.g. a work-day that refuses a second
  arrival, lateness derived on the entity). Business rules sit directly under unit test
  with no infrastructure — the heart of the project.
- **Use-case orchestration — application/domain services.** Plain interfaces in Core
  (`ITapService`, `IScheduleService`) implemented as services and called directly by the
  API. Keeps Core dependency-free and DI trivial; no mediator library given the small
  use-case count. (Revisit Wolverine only if messaging + use-case handling should unify.)
- **Time — `TimeProvider` (.NET 8+).** Injected into the domain so arrival/lateness logic
  is deterministic; tests freeze/advance time with `FakeTimeProvider`
  (`Microsoft.Extensions.TimeProvider.Testing`). Revisit NodaTime only if multi-timezone
  schedules become a real requirement.
- **IDs & values — primitives on records.** `Guid`/`string` fields on record entities;
  clarity via naming (`CardUid`, `UserId`). Vogen strongly-typed IDs noted as a future
  upgrade if compile-time type safety is wanted.

### TimeTracker.Infrastructure
- **Data access — repository interfaces in Core, EF impls here.** `IUserRepository` etc.
  defined in Core, implemented over `DbContext`; `DbContext` itself is the Unit of Work
  (`SaveChangesAsync`). Keeps the domain testable (Tests mock the interfaces) and Core
  free of EF.
- **Mapping — Fluent API via `IEntityTypeConfiguration<T>`.** One config class per entity,
  auto-discovered with `ApplyConfigurationsFromAssembly`. Core entities stay clean
  POCOs/records with no EF attributes.
- **Messaging — raw `RabbitMQ.Client` v7 behind `IEventPublisher`.** A thin publisher
  implementing a Core interface (mockable in Tests). Spec only needs to publish events, so
  no framework; MassTransit is the upgrade path if consumers/retries/outbox are needed.
- **Migrations — applied at startup via `Database.Migrate()`.** Simplest fit for
  `docker-compose` (wait for Postgres healthy, then migrate). Acceptable given the
  infrastructure layer is intentionally not production-grade; EF migration bundles noted as
  the production upgrade.

### TimeTracker.Tests
- **Test data — Test Data Builders.** Fluent builders producing a valid-by-default
  entity/command with targeted overrides (`new WorkDayBuilder().WithTwoTaps().Build()`).
  Fits the rich domain model whose methods enforce invariants; keeps tests
  intention-revealing. Object Mother noted as the lighter alternative.
- **Structure & naming — AAA + `Method_Scenario_Expected`.** Arrange/Act/Assert with names
  like `Tap_SecondArrivalSameDay_Throws`. The .NET community default; low ceremony.
- **Parameterized cases — `[Theory]` + `TheoryData<T>`.** Type-safe case tables for the
  rule boundaries (lateness grace, schedule exclusions); `[InlineData]` for trivial
  one-liners.
- **Assertions — AwesomeAssertions.** Free MIT fork of FluentAssertions v7 with identical
  `.Should()` syntax — chosen because FluentAssertions v8+ moved to a paid commercial
  license. Supersedes the FluentAssertions line under **Tooling** (pinning FluentAssertions
  v7 is the equivalent fallback).

### Open questions / upgrade paths

**Open questions — to confirm during implementation:**
- **FastEndpoints vs built-in validation.** FastEndpoints ships its own
  FluentValidation-based validation; we plan to opt out of it and use the .NET 10 built-in
  path. Confirm the opt-out mechanism and that DataAnnotations on request DTOs flow through
  the FastEndpoints pipeline as expected.
- **Startup migrations.** Whether `Database.Migrate()` should be gated to dev/compose only
  (vs running unconditionally on boot).
- **Event-publishing reliability.** Current design is fire-and-forget with no outbox — if a
  tap is committed but the RabbitMQ publish fails, the event is lost. Acceptable for a lean
  test project; revisit if delivery guarantees matter.

**Upgrade paths — deliberately lean now, swap later if scope grows:**
- **Orchestration:** application/domain services → **Wolverine** (unify use-case handling +
  messaging).
- **Time:** `TimeProvider` → **NodaTime** (multi-timezone schedules, DST correctness).
- **IDs & values:** primitives on records → **Vogen** strongly-typed IDs.
- **Messaging:** raw `RabbitMQ.Client` → **MassTransit** (consumers, retries, outbox).
- **Migrations:** startup `Database.Migrate()` → **EF migration bundles** (`efbundle`).
- **Test data:** Test Data Builders ↔ **Object Mother** (lighter, if builders feel heavy).
- **Assertions:** **AwesomeAssertions** ↔ pinned **FluentAssertions v7** (interchangeable
  free options).

## Demo
A separate small **Blazor** web app, `TimeTracker.Demo`, that drives the API through the
full scenario so you can *see* it working in a browser. Structured like the main solution:
its own Solution shape, Tooling, and Architectural decisions.

### Solution shape
One project, joining the same solution and `docker-compose.yml` as a long-running web
service (not a one-shot like the tests container).

| Project | Responsibility |
|---|---|
| `TimeTracker.Demo` | Blazor web app (Interactive Server) that calls the API to walk the full story — register a user + card, tap arrival/departure, trigger the double-tap `409`, set a work schedule, add an exclusion, and render the resulting history |

### Tooling
- **TFM:** `net10.0` (same as the rest of the solution)
- **UI:** Blazor Web App template, **Interactive Server** render mode; built-in Blazor
  components only (forms, buttons, a history table) — no component library
- **API calls:** typed `HttpClient` via `IHttpClientFactory` + `System.Net.Http.Json`
  (`PostAsJsonAsync` / `GetFromJsonAsync`), configured with the API's `SnakeCaseLower` JSON
  policy so the demo speaks the real wire format
- **Contracts:** local request/response `record`s in the demo (no shared project)
- **Config:** API base URL from `appsettings.json` / env (`ApiBaseUrl`) — `http://api:8080`
  under compose, any URL when run standalone
- **Containers:** multi-stage `Dockerfile`; added as a `demo` service in
  `docker-compose.yml` (`depends_on` api healthy), exposed on a host port to browse

### Architectural decisions
- **Hosting / render model — Blazor Web App, Interactive Server.** UI runs server-side over
  a SignalR circuit and calls the API from the server, so there's **no CORS** to configure
  and **no separate client project** — a single container. Interactive WebAssembly/Auto
  noted as the upgrade for a true browser-side SPA (would need CORS on the API + a client
  project).
- **API access — typed `HttpClient` + `System.Net.Http.Json`.** Named/typed client pointing
  at `ApiBaseUrl`, reusing `SnakeCaseLower`. Zero extra dependencies; Refit or a
  Kiota/NSwag generated client noted as upgrades if typed-interface readability or an
  OpenAPI-driven client is wanted.
- **Contracts — local records.** A handful of request/response records in the demo keep it
  self-contained with no coupling to `Api`. A shared `TimeTracker.Contracts` project is the
  no-duplication upgrade (and would also tidy the API).
- **Run / orchestration — long-running web container + standalone.** The `demo` compose
  service waits for the API to be healthy, then serves the UI on a host port for the
  turnkey `docker compose up` story; the configurable `ApiBaseUrl` also lets it run
  standalone via `dotnet run`. (Unlike the tests container, it's long-running, not
  one-shot.)
- **Scope — happy path + the one telling failure.** Demonstrates the full success flow plus
  the double-tap `409` (surfacing the API's ProblemDetails message in the UI). Minimal
  validation/empty-state handling — it's a demo, not a product; MudBlazor noted as an
  optional polish upgrade.
