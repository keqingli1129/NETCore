# NZWalks.MVC

## Project Overview

ASP.NET Core MVC web application targeting **.NET 10** (C# 14). Uses the default MVC template with controllers and Razor views.

This project is the **front end half of the NZWalks stack** inside `NETCore.sln`. It consumes `NZWalks.API` through a typed client generated at build time by NSwag from a committed OpenAPI document, and ships a full Regions CRUD UI. It still has no DbContext and no authentication.

## Related projects

`NETCore.sln` (repo root) holds three independent sample stacks. This project belongs to the NZWalks one and does not reference the others:

| Project | Relationship |
|---|---|
| `NZWalks.API` | The data source for this app's Regions CRUD UI, consumed through a typed client generated at build time by NSwag — see below. |
| `NZWalks.API.Tests` | Tests the API only. There is no test project for this MVC app. |
| `CoreMVC.*`, `PlainNetCore*` | Separate stacks with their own architectures. See the root `CLAUDE.md`; don't copy patterns across without a reason. |

Note that `PlainNetCoreMVC` already solves the "MVC app calling a sibling Web API" problem — typed `HttpClient` registered against a `BaseUrl` config value, plus a checked-in NSwag-generated client. Read it before hand-rolling a different approach here.

### NZWalks.API

- Runs at `https://localhost:7223` / `http://localhost:5062`; Scalar UI at `/scalar/v1` (Development only).
- Controllers: `Regions`, `Walks`, `Difficulties`, `Auth`, plus scaffold leftovers (`Sudents`, `WeatherForecast`).
- JWT bearer auth is **configured but not enforced**: `Program.cs` wires `AddJwtBearer` and `Auth/Login` issues tokens, but no controller carries `[Authorize]`, so every endpoint is currently open. A consumer works without a token today — don't assume otherwise, and re-check if `[Authorize]` is added later.
- Request/response shapes live in `NZWalks.API/Models/DTOs` (`RegionDto`, `WalkDto`, `DifficultyDto`, `LoginRequestDto`, `LoginResponseDto`, `RegisterRequestDto`). Its EF entities are internal to the API; consume the DTOs, not the entities.
- Reads the `NZWalksConnectionString` connection string. This MVC project has no `ConnectionStrings` section at all and shouldn't grow one if it goes through the API.

## Refreshing the API contract

Codegen reads the committed `OpenAPIs/nzwalks.v1.json`, so the build never needs the API running. Refresh it after any change to the API's routes or DTOs:

```bash
dotnet run --project ./NZWalks.API
curl -sk "https://localhost:7223/openapi/v1.json" -o NZWalks.MVC/OpenAPIs/nzwalks.v1.json
```

Then rebuild and fix any compile errors in `ApiClients/RegionsApi.cs`, where generated names are referenced.

Known API-side behaviour: `PUT api/Regions/{id}` sets the image URL from the uploaded file unconditionally, so saving an edit without choosing a file clears the region's existing image. That lives in `NZWalks.API`, not here.

## Build & Run

```bash
cd NZWalks.MVC
dotnet build
dotnet run
```

From the repo root: `dotnet run --project ./NZWalks.MVC` (and `./NZWalks.API` for the API). Development ports for this app are `https://localhost:7000` / `http://localhost:5114`.

To actually exercise the Regions pages, `NZWalks.API` must also be running — every action in `RegionsController` calls out to it, and with it down the pages render the generic error banner instead of data (see "Refreshing the API contract" for the known image-clearing behaviour, and `NZWalksApi:BaseUrl` under Key Configuration below for the wiring). Start both:

```bash
dotnet run --project ./NZWalks.API   # https://localhost:7223
dotnet run --project ./NZWalks.MVC   # https://localhost:7000
```

## Project Structure

- `Controllers/` — MVC controllers: `HomeController` (scaffold), `RegionsController` (the Regions CRUD UI, dispatches through `IRegionsApi`)
- `ApiClients/` — the hand-written facade over the NSwag-generated client: `IRegionsApi.cs` / `RegionsApi.cs` (the single place generated method names like `RegionsAllAsync`/`RegionsGETAsync`/`RegionsPOSTAsync`/`RegionsPUTAsync`/`RegionsDELETEAsync` are referenced — see the Conventions note on churn) and `FileParameter.cs` (a hand-written stand-in for a class NSwag failed to emit for this spec; see the comment at the top of that file for why)
- `OpenAPIs/nzwalks.v1.json` — the committed OpenAPI document codegen reads at build time; see "Refreshing the API contract"
- `Models/` — `ErrorViewModel` (scaffold), `NZWalksApiOptions` (binds the `NZWalksApi` config section and resolves API-relative image paths to absolute URLs), `RegionFormViewModel` (the Create/Edit form model)
- `Views/` — Razor views organized by controller (`Home/`, `Regions/` — Index/Details/Create/Edit/Delete), plus Shared layout and the `_ApiError` partial used to render the generic API-failure banner
- `wwwroot/` — Static assets (CSS, JS, Bootstrap, jQuery)
- `Program.cs` — App entry point and middleware configuration; also where `NZWalksApiOptions` is bound and the typed `HttpClient` for the generated API client is registered

## Key Configuration

- **Target Framework:** net10.0
- **Nullable:** enabled
- **Implicit Usings:** enabled
- **Routing:** Convention-based (`{controller=Home}/{action=Index}/{id?}`)
- **`NZWalksApi:BaseUrl`** (currently `https://localhost:7223/`) — a hard startup dependency: `Program.cs` throws `InvalidOperationException("Configuration value 'NZWalksApi:BaseUrl' not found.")` if it's missing or blank. The trailing slash matters: it becomes the generated `HttpClient`'s `BaseAddress`, and the generated client appends relative paths like `api/Regions` to it — without the trailing slash, combining a base URI with a relative path can drop the base's last path segment.

## Conventions

- Follow standard ASP.NET Core MVC patterns (controllers return views, models are POCOs)
- Use `MapStaticAssets()` and `WithStaticAssets()` for static file serving
- Dependencies are limited to the NSwag codegen toolchain (`NSwag.ApiDescription.Client`, `Microsoft.Extensions.ApiDescription.Client`) plus `Newtonsoft.Json`, which generated client code requires at runtime.
- The API client is **generated into `obj/` and never committed** — unlike `PlainNetCoreMVC`, which checks its NSwag output into source control. The only committed artifact is `OpenAPIs/nzwalks.v1.json`.
- `RegionsController` depends on `IRegionsApi` (`ApiClients/`), a thin facade over the generated client; `HomeController` is unrelated scaffold and has no API dependency. Generated method names change whenever an API action is renamed; keep that churn inside `RegionsApi`.
- `Program` is a `public class Program` inside the `NZWalks.MVC` namespace, not top-level statements. The other hosts in the solution expose `Program` as a `public partial class` so `WebApplicationFactory<Program>` can reach it — match that shape if integration tests are added here.
