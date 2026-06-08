# TimeTracker

A small **employee work-time tracking microservice**. Workers identify themselves by tapping an NFC
card twice a day (arrival, then departure — the card identifies, it is not an access pass).
Administrators set work schedules, add schedule exclusions, and read attendance history and statistics.

> This is a deliberately lean **test project** — the domain and its unit tests are the focus, not
> production-grade infrastructure. For the in-browser demo UI, see [README_Demo.md](README_Demo.md).

## Architecture

Four projects with a strict inward dependency flow (`Core` depends on nothing else in the solution):

```
Core  ◄── Infrastructure   (EF Core + Npgsql, RabbitMQ publisher — implements Core's interfaces)
  ▲
  └────── Api              (FastEndpoints HTTP surface; maps domain exceptions → RFC 7807)
Tests ──► Core             (xUnit, mocked repositories, FakeTimeProvider)
```

- **`TimeTracker.Core`** — the unit-tested heart. Rich domain model (entities own their invariants;
  e.g. a third tap in a day throws `AlreadyTappedException`), plain application services, and
  repository / unit-of-work / event-publisher *interfaces only*. No EF, ASP.NET, or RabbitMQ here.
  Lateness / early-leave / under-work are derived once in `AttendanceEvaluator`, and statistics just
  aggregate its per-day results — so history and statistics never disagree.
- **`TimeTracker.Infrastructure`** — EF Core (`AppDbContext` = the unit of work) over PostgreSQL with
  code-first migrations, plus a publish-only RabbitMQ event publisher. A `CardTappedEvent` is published
  after each successful tap (fire-and-forget; publish failures are logged, not fatal).
- **`TimeTracker.Api`** — one FastEndpoints class per route. Endpoints stay on the happy path
  (bind DTO → call one Core service → return DTO); a single exception handler turns domain exceptions
  into ProblemDetails responses.

**Stack:** .NET 10 · PostgreSQL · RabbitMQ · FastEndpoints. The JSON wire format is **snake_case**
end-to-end (`card_uid`, `user_id`, `type_exclusion`, …). All endpoints are `POST` + `application/json`.

## Endpoints

| Route | Request fields | Returns |
|---|---|---|
| `/card/touch` | `card_uid` | `{ card_uid, user_id }` |
| `/card/assign` | `user_id`, `card_uid` | `{ card_uid, user_id }` |
| `/card/delete` | `card_uid` | `{ card_uid, user_id }` |
| `/card/list_by_user` | `user_id` | `{ user_id, cards[] }` |
| `/card/delete_all_by_user` | `user_id` | `{ user_id, cards[] }` |
| `/work_time/set` | `user_id`, `start_time`, `end_time`, `days`, `free_schedule` | the stored schedule |
| `/work_time/get` | `user_id` | the stored schedule |
| `/work_time/add_exclusion` | `user_id`, `type_exclusion`, `start_datetime`, `end_datetime` | the stored exclusion |
| `/work_time/get_exclusion` | `user_id` | `{ user_id, exclusions[] }` |
| `/work_time/history_by_user` | `user_id`, `filter`* | attendance entries |
| `/work_time/history` | `limit`, `filter`* | attendance entries (all workers) |
| `/work_time/statistics_by_user` | `user_id`, `filter`* | one worker's statistics |
| `/work_time/statistics` | `limit`, `filter`* | totals + per-worker statistics |

`type_exclusion` ∈ `"arrive later"`, `"leave earlier"`, `"full working day"`.

\* **`filter`** is the statistics/history period: `"week"`, `"month"`, `"year"`, or `"entire period"`.
It is optional and **defaults to `"month"`**. An unknown value returns `400`.

**Statistics** report, per the selected period: required vs. worked vs. under-worked minutes, and the
number of times the worker was late / left early — split into *with reason* (covered by an exclusion)
and *without reason*.

## Running

```bash
# Full stack: postgres + rabbitmq + api  (add the demo too — see README_Demo.md)
docker compose up --build api

# Swagger UI:   http://localhost:8080/swagger
# Health check: http://localhost:8080/health
```

Example tap:

```bash
curl -X POST http://localhost:8080/card/touch \
  -H 'Content-Type: application/json' \
  -d '{"card_uid":"04-A1-B2-C3"}'
# → { "card_uid": "04A1B2C3", "user_id": 1001 }
```

## Build & test

```bash
dotnet build                                   # builds TimeTracker.slnx
dotnet test                                    # runs the Core unit tests
dotnet test --filter "FullyQualifiedName~StatisticsServiceTests"   # one class

# EF Core migrations (dotnet-ef is pinned in dotnet-tools.json)
dotnet tool restore
dotnet ef migrations add <Name> -p src/TimeTracker.Infrastructure -s src/TimeTracker.Api -o Migrations
```

Configuration (connection strings, RabbitMQ host/exchange, lateness thresholds) is bound from
`appsettings.json` and environment variables; see `docker-compose.yml` for the wired-up values.
