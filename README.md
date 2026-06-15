# Mini Condition-Monitoring App

A small but real condition-monitoring vertical slice. A .NET 8 backend ingests simulated
time-series sensor data from a generator asset, a fault detector raises alarms when a
brush-length sensor drops below its threshold and when vibration trends abnormally, and a
React dashboard shows live trend charts and acknowledgeable alarms.

This is an interview and portfolio demo, not a production system. It is optimized for a
clean, fully explainable slice rather than breadth of features.

## What you see when it runs

Start the API and the web app, open the dashboard, and within about a minute:

1. The charts are already populated with roughly two minutes of healthy history (seeded as a
   backfill so the page is not empty on first load).
2. After roughly 35 seconds the vibration z-score flags an abnormal upward trend and a
   `Warning` appears, while vibration is still well below its hard limit.
3. After roughly a minute the brush-length line crosses its `30%` threshold and a red
   `Critical` alarm appears live.
4. Vibration keeps climbing and eventually crosses its `7 mm/s` limit, raising a second
   `Warning`.
5. Clicking `Acknowledge` clears an alarm and it stays cleared. The asset status badge
   updates from `Alarm` back toward `Healthy` as alarms are acknowledged.

## Prerequisites

- .NET 8 SDK. A newer SDK also works as long as the .NET 8 runtime is installed, since the
  projects target `net8.0`.
- Node.js 18 or newer (developed against Node 22) and npm.

## Run the API

```
cd Cm.Api
dotnet run
```

- API base URL: `http://localhost:5080`
- Swagger UI: `http://localhost:5080/swagger`

On first start it creates a local SQLite file (`cm.db`) with `EnsureCreated`, seeds one
`Generator` asset and two sensors, backfills about two minutes of healthy readings, then
begins the once-per-second simulation. Delete `cm.db` to start over from a clean baseline.

## Run the web app

In a second terminal:

```
cd web
npm install
npm run dev
```

- Dashboard: `http://localhost:5173`

The web app polls the API every two seconds. If you host the API somewhere other than the
default, set `VITE_API_BASE` (for example `http://localhost:5080/api`) before `npm run dev`.

## Run the tests

```
dotnet test
```

The `Cm.Api.Tests` project unit-tests the fault detector (the low-brush threshold, the
vibration z-score trend, the divide-by-zero guard, and the alarm dedupe).

## REST API

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/assets` | List assets with derived status, sensor count, and active-alarm count |
| GET | `/api/assets/{id}` | Asset detail with sensors and the current value per sensor |
| GET | `/api/sensors/{id}/readings?minutes=10` | Readings in the last N minutes, ordered by time |
| POST | `/api/sensors/{id}/readings` | Ingest a single reading (`value`, optional `timestamp`), returns 201 |
| GET | `/api/alarms?active=true` | List alarms, unacknowledged by default |
| POST | `/api/alarms/{id}/acknowledge` | Acknowledge an alarm, returns 204, or 404 if missing |
| GET | `/api/assets/{id}/health-summary` | Grounded health summary plus one proposed action |

## Project structure

```
/Cm.Api          ASP.NET Core Web API (controllers), EF Core + SQLite, the simulation
/Cm.Api.Tests    xUnit tests for the fault detector
/web             Vite + React + TypeScript + recharts dashboard
```

## Design notes

These are the decisions worth calling out, with the reasoning behind them.

- **Derived status, not stored.** An asset has no status column. Its status is computed from
  active alarms at read time (`AssetStatusCalculator`): any active `Critical` means `Alarm`,
  otherwise any active `Warning` means `Warning`, otherwise `Healthy`. There is one source of
  truth and it cannot drift.

- **Per-tick DI scope in the background service.** The simulation is a singleton
  `BackgroundService`, but `DbContext` is scoped and not thread-safe. Each tick opens a fresh
  DI scope with `IServiceScopeFactory` and resolves the `DbContext` inside it. This is the
  standard way to use EF Core from a long-lived hosted service.

- **RMS is a derived feature.** Each tick builds a short synthetic waveform (a sine plus
  gaussian noise) and reduces it to a single trendable feature, the RMS. Deriving a feature
  like RMS from a raw signal mirrors how real platforms turn raw vibration into something you
  can trend and alarm on.

- **Alarm dedupe and edge-triggering.** A fault is raised only as its condition first becomes
  true (edge-triggered), and never if an unacknowledged alarm of the same type already exists
  for that sensor (dedupe). Together these mean a sustained fault raises exactly one alarm,
  and acknowledging it clears it for good rather than having it re-raise on the next tick.

- **DTOs avoid serialization cycles.** Controllers return small response DTOs, never the EF
  entities. This keeps the API contract explicit and sidesteps navigation-property
  serialization cycles.

- **Timestamps are UTC.** SQLite does not preserve `DateTimeKind`, so timestamps are stamped
  UTC at the serialization boundary (`TimeUtil`) and the browser parses them correctly.

- **SQLite in WAL mode.** Write-ahead logging is enabled once at startup so the one-per-second
  writer and the dashboard's polling reads do not block each other.

- **The ML seam.** `IFaultDetector` is the boundary between raw data and fault decisions. The
  shipped `RuleBasedFaultDetector` is classical and explainable. In production a trained model
  would sit behind this interface, typically as a separate Python service called over HTTP.

- **The LLM seam.** `IHealthSummarizer` produces a plain-language summary and a single
  proposed action. The default `TemplatedHealthSummarizer` is deterministic and needs no API
  key, so the repo runs offline with no secrets. The code marks exactly where a real LLM call
  would go, passing only the grounded context (recent readings and active alarms) into the
  prompt and returning a proposed action that a human approves. The action is never executed
  automatically, and the Approve button in the UI only flips a label.

- **Polling now, SignalR later.** The dashboard polls every two seconds. That is plenty for a
  demo and keeps the moving parts minimal. A production build would likely push updates over
  SignalR or WebSockets instead, but the data shapes would not change.

## How this maps to a real condition-monitoring platform

The domain model is the same shape you would find in a real platform. **Assets** (equipment)
have **Sensors** (measurement channels), each Sensor produces a stream of **Readings** (the
time-series), and fault logic raises **Alarms** against an asset and sensor. Raw signals are
reduced to **derived features** such as RMS that are cheap to store and meaningful to trend.

Fault detection sits behind a single interface so the simple rules here can be swapped for a
**trained ML model** without touching the ingestion or API code. That model would usually run
as its own service (often Python) and be called over HTTP, which is exactly the boundary the
`IFaultDetector` interface draws. A second interface does the same for a **grounded LLM
summary**: the recent readings and active alarms are the only context passed in, and anything
the model proposes is gated behind human approval rather than acted on automatically.

The live view uses **polling** today for simplicity. Moving to **SignalR** or WebSockets later
would make updates push-based, but the asset, sensor, reading, and alarm contracts stay the
same.
