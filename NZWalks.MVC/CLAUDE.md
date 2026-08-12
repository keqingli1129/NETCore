# NZWalks.MVC

## Project Overview

ASP.NET Core MVC web application targeting **.NET 10** (C# 14). Uses the default MVC template with controllers and Razor views.

This project is the **front end half of the NZWalks stack** inside `NETCore.sln`. It is currently an unmodified scaffold — no DbContext, no authentication, and no client for its paired API yet. Anything in those areas is new work, not a modification of existing wiring.

## Related projects

`NETCore.sln` (repo root) holds three independent sample stacks. This project belongs to the NZWalks one and does not reference the others:

| Project | Relationship |
|---|---|
| `NZWalks.API` | The intended data source for this app — see below. No reference or HTTP client exists yet. |
| `NZWalks.API.Tests` | Tests the API only. There is no test project for this MVC app. |
| `CoreMVC.*`, `PlainNetCore*` | Separate stacks with their own architectures. See the root `CLAUDE.md`; don't copy patterns across without a reason. |

Note that `PlainNetCoreMVC` already solves the "MVC app calling a sibling Web API" problem — typed `HttpClient` registered against a `BaseUrl` config value, plus a checked-in NSwag-generated client. Read it before hand-rolling a different approach here.

### NZWalks.API

- Runs at `https://localhost:7223` / `http://localhost:5062`; Scalar UI at `/scalar/v1` (Development only).
- Controllers: `Regions`, `Walks`, `Difficulties`, `Auth`, plus scaffold leftovers (`Sudents`, `WeatherForecast`).
- JWT bearer auth is **configured but not enforced**: `Program.cs` wires `AddJwtBearer` and `Auth/Login` issues tokens, but no controller carries `[Authorize]`, so every endpoint is currently open. A consumer works without a token today — don't assume otherwise, and re-check if `[Authorize]` is added later.
- Request/response shapes live in `NZWalks.API/Models/DTOs` (`RegionDto`, `WalkDto`, `DifficultyDto`, `LoginRequestDto`, `LoginResponseDto`, `RegisterRequestDto`). Its EF entities are internal to the API; consume the DTOs, not the entities.
- Reads the `NZWalksConnectionString` connection string. This MVC project has no `ConnectionStrings` section at all and shouldn't grow one if it goes through the API.

## Build & Run

```bash
cd NZWalks.MVC
dotnet build
dotnet run
```

From the repo root: `dotnet run --project ./NZWalks.MVC` (and `./NZWalks.API` for the API). Development ports for this app are `https://localhost:7000` / `http://localhost:5114`.

## Project Structure

- `Controllers/` — MVC controllers (HomeController)
- `Models/` — View models (ErrorViewModel)
- `Views/` — Razor views organized by controller, plus Shared layout
- `wwwroot/` — Static assets (CSS, JS, Bootstrap, jQuery)
- `Program.cs` — App entry point and middleware configuration

## Key Configuration

- **Target Framework:** net10.0
- **Nullable:** enabled
- **Implicit Usings:** enabled
- **Routing:** Convention-based (`{controller=Home}/{action=Index}/{id?}`)

## Conventions

- Follow standard ASP.NET Core MVC patterns (controllers return views, models are POCOs)
- Use `MapStaticAssets()` and `WithStaticAssets()` for static file serving
- No external NuGet dependencies beyond the SDK
- `Program` is a `public class Program` inside the `NZWalks.MVC` namespace, not top-level statements. The other hosts in the solution expose `Program` as a `public partial class` so `WebApplicationFactory<Program>` can reach it — match that shape if integration tests are added here.
