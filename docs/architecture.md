# Architecture

Liturgiek Statistiek is a Clean Architecture .NET 10 Web API serving an Angular 21
zoneless single-page application.

## High-level

```
Angular 21 SPA (zoneless)  →  .NET 10 Web API  →  EF Core  →  SQL Server / Azure SQL
                                   │
                                   └── Azure OpenAI (optional, for NL queries & parsing)
```

## Backend layers

- **Domain** (`LiturgiekStatistiek.Domain`) — entities (`Service`, `ServiceElement`,
  `ServiceElementSong`, `ReadingReference`, `ServiceTemplate`, `ServiceTemplateElement`,
  `Congregation`, `Preacher`, `ListItem`/`ListDefinition`), enums (`TimeOfDay`,
  `ServiceStatus`), and interfaces (`IApplicationDbContext`).
- **Application** (`LiturgiekStatistiek.Application`) — DTOs, service interfaces
  (`IServiceService`, `ITemplateService`, `IQueryService`, `IAdvancedQueryService`,
  `ISavedQueryService`, `ILlmService`) and pure logic (`SongCompletenessCalculator`).
  No infrastructure dependencies.
- **Infrastructure** (`LiturgiekStatistiek.Infrastructure`) — EF Core
  `ApplicationDbContext`, `DataSeeder`, and service implementations.
- **Api** (`LiturgiekStatistiek.Api`) — controllers, DI wiring, auth, CORS, Swagger.

In development the API uses an **in-memory database** seeded with sample services and
authentication is disabled (a `DevAuthHandler` auto-authenticates every request, matching
the frontend `devBypass`).

## Authentication & authorization

The whole app is **behind login** (Entra ID / MSAL). There are **no roles** — a user is
either signed in (full editor) or anonymous (no access). The Angular homepage is a branded
login screen and every route except `/` is guarded (`authGuard`); all mutating API
endpoints require `[Authorize]`. Locally, `devBypass`/`DevAuthHandler` short-circuit MSAL so
the app is usable without Entra.

## Service lifecycle

Services have a `Status` (`Concept`/`Gepubliceerd`). Editing works against a server-side
draft that is **autosaved**; **Publiceren** flips it to `Gepubliceerd`. Concepts are hidden
from all queries/stats until published. When a service's gemeente/voorganger changes or the
service is deleted, an orphan `Congregation`/`Preacher` (0 services left) is hard-deleted and
dropped from entry dropdowns.

## Frontend: zoneless change detection

The Angular app runs **zoneless** (no `zone.js`). This means change detection is not
triggered automatically after async work completes. The application therefore follows
two rules:

1. **Async-updated state is stored in signals.** Values set inside RxJS `subscribe()`
   callbacks (e.g. HTTP responses) are written with `signal.set(...)`, which schedules
   change detection. Plain class fields mutated inside a `subscribe()` would **not**
   re-render the view — this was the original cause of "pages that load slowly / never
   render until interaction".
2. **User-event-driven state** (template `(click)`/`ngModel` handlers) triggers change
   detection automatically because the event originates from the template, so plain
   fields are acceptable for builder-style UIs (e.g. the advanced query builder rows).

`provideZonelessChangeDetection()` is registered in `app.config.ts`.

## Testing

- `tests/LiturgiekStatistiek.UnitTests` — NUnit unit tests (controllers, services,
  domain). Uses Moq for `IApplicationDbContext`/service mocks.
- `tests/LiturgiekStatistiek.IntegrationTests` — NUnit tests against a real
  EF Core in-memory `ApplicationDbContext` seeded via `DataSeeder`.
- Frontend — Vitest (`npm test`).
