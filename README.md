# WorkoutTracker.Server

ASP.NET Core 9 minimal API backing the WorkoutTracker clients: a React web app, a React 19
admin console, and a React Native (Expo) mobile app. EF Core over SQLite, ASP.NET Identity
with JWT bearer auth and refresh-token rotation.

## Architecture

Requests flow `Routes → Handlers → Repositories → EF Core`, with DTOs at the edges and
domain objects doing the conversion:

| Layer | Location | Responsibility |
| --- | --- | --- |
| Routes | `Routes/*.cs` | Endpoint definitions and auth policies, registered from `Program.cs` |
| Handlers | `Handlers/*.cs` | Business logic, returns `IResult` |
| Repositories | `Models/Repositories/` | Data access, registered in `RespositoryService.cs` |
| Domain objects | `DomainObjects/` | `FromModel` / `ToModel` / `FromData` / `ToData` conversions |
| DTOs | `DataTransferObjects/{Common,Requests,Responses}/` | Wire contracts mirrored by the clients |

Cross-cutting pieces: `Middleware/RequestLoggingMiddleware` logs every request to the
database, `Logging/DatabaseLoggerProvider` persists log entries, `Authorization/` holds the
resource-ownership handlers, and `RefreshTokenCleanupService` prunes expired tokens.

## Running locally

```bash
dotnet restore
dotnet run
```

The server listens on `http://localhost:5231`. The SQLite file is created and migrated on
first start; `dotnet ef migrations add <Name>` adds a migration.

### Required configuration

**No secrets are committed to this repository.** `appsettings.json` ships the non-secret
shape only, and the server refuses to start without a signing key. Copy the example and
fill it in:

```bash
cp appsettings.Development.local.example.json appsettings.Development.local.json
```

`appsettings.Development.local.json` is gitignored. It supplies:

| Key | Purpose |
| --- | --- |
| `Jwt:Secret` | **Required.** JWT signing key, at least 32 bytes. Startup throws without it. |
| `AdminSeed:Email` / `AdminSeed:Password` | Optional. Seeds an account with the `Admin` role. Both must be set or nothing is seeded. The configured password is authoritative — it is reapplied to an existing account on startup. |
| `DevSeed:Email` / `DevSeed:Password` | Optional, Development only. Seeds a user with six weeks of sample workouts, templates and a plan. Also enables the mobile app's "Use dev account" button. |

In production these are App Service settings using the double-underscore form:
`Jwt__Secret`, `AdminSeed__Email`, `AdminSeed__Password`.

## Auth

`POST /auth/login` returns a short-lived access token (15 min) plus a refresh token
(7 days); `POST /auth/refresh` rotates them and `POST /auth/logout` revokes. Identity's
password policy is deliberately relaxed for demo convenience — see `Program.cs`.

Every route requires authentication except `POST /auth/login`, `POST /auth/refresh` and
`GET /monitoring/status`. Everything under `/admin` requires the `AdminOnly` policy, which
includes log access at `GET /admin/logs`.

## Tests

```bash
dotnet test
```

xUnit tests in `WorkoutTracker.Server.Tests/` cover the handlers, `JwtService` and the
authorization rules against an in-memory database.

## Deployment

Pushing to `master` builds and deploys to Azure App Service via
`.github/workflows/master_workouttracker-server.yml`, authenticating with OIDC. The
workflow runs no test gate, so a merge to `master` is a live deploy. Production keeps its
SQLite file at `/home/WorkoutTracker.db`, on the persistent share above `site/wwwroot`, so
deploys do not touch data.
