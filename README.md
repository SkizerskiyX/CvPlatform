# CV Platform

Coursework: web platform for CV management. Backend: ASP.NET Core Web API, C#, PostgreSQL, EF Core. Frontend: React SPA (Vite + TypeScript) served by the API.

## Projects

- `CvPlatform.Domain` - entities, enums, domain exceptions. Guarded constructors, named state-change methods, internal child constructors.
- `CvPlatform.Application` - DTO records, repository interfaces, `IUnitOfWork`, `AttributeValueWriter`, `AccessRuleEvaluator`, services, FluentValidation validators.
- `CvPlatform.Infrastructure` - `AppDbContext`, Fluent API configurations, repository implementations, `UnitOfWork`, `AddInfrastructure` (Npgsql, Identity, repositories, services, validators).
- `CvPlatform.API` - JWT + Google/Facebook OAuth, CORS for the SPA, controllers returning `ActionResult<T>`, serves the built SPA (`CvPlatform.Web/wwwroot`).
- `CvPlatform.Web` - React SPA sources (`src/`, Vite build into `wwwroot/`, `VITE_API_URL` from `.env`).

## Run with one command

```bash
dotnet run --project CvPlatform.API
```

The build automatically runs `npm install` (if `node_modules` is missing) and `npm run build` in `CvPlatform.Web`, then the API serves the frontend from its own address. Local API address: `http://localhost:5242` (see `CvPlatform.API/Properties/launchSettings.json`).

For frontend development:

```bash
cd CvPlatform.Web && npm run dev
```

Vite runs on `http://localhost:5173` and proxies `/api` to `http://localhost:5242`. Build the production bundle with `npm run build` (output: `wwwroot/`). Disable the MSBuild SPA step with `dotnet build /p:SpaBuildEnabled=false`.

## Run with Docker

```bash
docker compose up --build
```

Services: `postgres` (init in `pgdata`), `api` (API + SPA on `http://localhost:7000`, waits for a healthy postgres). Seeding runs on startup: `Experience/Skills/Education` categories, sample positions, `admin@cv.local / Admin123!` in the `Admin` role.

## Local database

Connection string: `ConnectionStrings:CvPlatform` in `CvPlatform.API/appsettings.json` (user secrets or environment variables in real deployments). Apply migrations:

```bash
dotnet ef database update --project CvPlatform.Infrastructure --startup-project CvPlatform.API
```

JWT and OAuth settings live in `CvPlatform.API/appsettings.json` (`Jwt`, `OAuth`). Frontend API base URL is `VITE_API_URL` in `CvPlatform.Web/.env` (empty means same origin; `.env` is git-ignored, see `.env.example`).

## Main flows

- Register/login (`POST /api/account/register`, `POST /api/account/login` returns `{ token, email, profileId }`), Google/Facebook external login with JWT redirect to the frontend (`/oauth-callback#token=...`).
- Profiles: `GET /api/profiles/me`, basic info, attributes (autosaved from the UI, stale `RowVersion` returns 409), projects, attribute values.
- Positions: public list/details; Admin creates/duplicates/attaches attributes and access rules.
- Attribute library: search by name prefix and category, CRUD (Admin).
- CVs: `POST /api/cvs/generate/{positionId}`, inline attribute editing, project inclusion, publish (validates required attributes), likes (`/api/likes/{cvId}`).
- Discussions: `GET/POST /api/discussions/position/{positionId}` with UI polling every 3 seconds.
- Roles: `GET /api/roles`, assign/remove (`Admin`), self-removal allowed.