# TimeTracker Demo

`TimeTracker.Demo` is a small **Blazor Web App (Interactive Server)** that drives the real
[TimeTracker API](README.md) from a browser, so you can exercise the whole service without writing
HTTP calls by hand. It is a client of the running API only — it shares no project references with
Core/Infrastructure and makes its calls server-side (no CORS, no WebAssembly).

## What it does

- **Admin panel** — pick a worker, set their schedule, add schedule exclusions, assign/list/delete
  cards, and read history and statistics. The history and statistics views each have a **period
  selector** (`week` / `month` / `year` / `entire period`) wired to the API's `filter`.
- **Generate panel** — seed demo data (users, cards, and back-dated taps) so the history and
  statistics views have something multi-day to show.
- **Request log** — every API call and its result (or the API's ProblemDetails error) is shown live,
  so you can see exactly what the service returned.

## Seeding (dev-only API endpoints)

The 13 spec endpoints can't create a user or record a tap at a past date, so the API exposes two
**demo-only, environment-gated** helper routes that the Generate panel uses:

- `POST /dev/create_user` — insert a user row (there is no spec create-user endpoint).
- `POST /dev/seed_tap` — record a tap at an explicit instant (to backfill multi-day history).

Both are active only in the `Development` environment (or with `EnableSeedEndpoints=true`) and return
`404` otherwise — which the demo reads as "seeding off, use live taps instead". They are not part of
the service contract.

## Running

```bash
# Start the full stack including the demo (also brings up postgres, rabbitmq, api)
docker compose up --build

# Demo UI:    http://localhost:8090
# API/Swagger: http://localhost:8080/swagger
```

The demo reaches the API via the `ApiBaseUrl` setting (`http://api:8080` inside Compose). To run it
against a locally hosted API instead, point `ApiBaseUrl` at that address.

> Tip: open the demo on a fresh database, use the **Generate** panel first to seed a few workers and
> back-dated taps, then switch to **Admin** to see history and statistics populate across periods.
