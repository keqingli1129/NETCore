# NZWalks.MVC → NZWalks.API client

**Date:** 2026-08-12
**Status:** Approved

## Goal

Give `NZWalks.MVC` a typed client for `NZWalks.API`, generated at build time from the API's OpenAPI document, and a full Regions CRUD UI built on it. Today `NZWalks.MVC` is an untouched `dotnet new mvc` scaffold with no dependencies, no DbContext and no way to reach its sibling API.

## Context

`NZWalks.API` runs at `https://localhost:7223` / `http://localhost:5062` and exposes:

| Route | Operations | Notes |
|---|---|---|
| `api/Regions` | GET all, GET by id, POST, PUT, DELETE | **POST and PUT are `multipart/form-data`** (image upload via `IFormFile`) |
| `api/Walks` | GET all, GET by id, POST, PUT, DELETE | JSON |
| `api/Difficulties` | GET all, GET by id, POST, PUT, DELETE | JSON |
| `api/Auth/Register`, `api/Auth/Login` | POST | Issues JWTs |
| `api/Sudents`, `WeatherForecast` | — | Scaffold leftovers |

Two facts that shaped the design:

- **Auth is configured but not enforced.** `Program.cs` wires `AddJwtBearer` and `AuthController` issues tokens, but no controller carries `[Authorize]`. Every endpoint is open, so the client needs no token today.
- **Uploaded images live in the API's `wwwroot`.** `RegionsController.SaveImageAsync` writes to `wwwroot/images/regions/` and returns a host-relative path like `/images/regions/{guid}.png`. Rendered verbatim from the MVC app that resolves against `localhost:7000` and 404s.

`PlainNetCoreMVC` is the in-repo precedent for an MVC app calling a sibling API. Inspecting its generated client establishes the NSwag options in use: relative URLs via `urlBuilder_.Append("api/Categories")` with no `BaseUrl` property (so `/UseBaseUrl:false`, relying on `HttpClient.BaseAddress`) and an `I`-prefixed interface (`/GenerateClientInterfaces:true`). Its generated `GetWeatherForecastAsync` confirms scaffold controllers end up in the client.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Client production | Build-time `OpenApiReference` codegen | Client always matches the committed contract; no large generated blob in source control. |
| Generated output | Not checked in — compiled from `obj/` | Differs deliberately from `PlainNetCoreMVC`, which commits its client. |
| Scope | Client + DI + full Regions CRUD UI | Exercises both the JSON and multipart paths end-to-end. |
| Auth | Omitted | No endpoint requires a token. `Login`/`Register` remain available on the generated client. |
| Error handling | Friendly in-page error | Keeps the app usable when the API is down. No retry policy, no typed 404/409 mapping. |
| Tests | No new test project | Consistent with the stack: `NZWalks.API.Tests` is an empty placeholder and PlainNetCore has none. Verification is manual. |

## Architecture

```
NZWalks.MVC                                  NZWalks.API
├── OpenAPIs/nzwalks.v1.json  ──codegen──┐   (https://localhost:7223)
├── obj/…/NZWalksApiClient.cs  ◀─────────┘
│      INZWalksApiClient (generated)
├── Program.cs
│      AddHttpClient<INZWalksApiClient, NZWalksApiClient>
│        BaseAddress = NZWalksApi:BaseUrl  ──HTTP──▶  api/Regions
├── Controllers/RegionsController.cs
├── Models/RegionFormViewModel.cs
├── Models/NZWalksApiOptions.cs
└── Views/Regions/{Index,Details,Create,Edit,Delete}.cshtml
```

The generated client is the only thing that knows the API's wire format. Controllers depend on `INZWalksApiClient`, never on `HttpClient`. Views depend on generated DTOs for display and on `RegionFormViewModel` for input.

### Codegen wiring

`NZWalks.MVC.csproj` gains:

- `NSwag.ApiDescription.Client` 14.4.0 — `PrivateAssets=all`
- `Microsoft.Extensions.ApiDescription.Client` 9.0.0 — `PrivateAssets=all`
- `Newtonsoft.Json` 13.0.3 — runtime dependency of generated code

```xml
<OpenApiReference Include="OpenAPIs\nzwalks.v1.json"
                  ClassName="NZWalksApiClient"
                  Namespace="NZWalks.MVC.ApiClients">
  <SourceUri>https://localhost:7223/openapi/v1.json</SourceUri>
  <Options>/GenerateClientInterfaces:true /UseBaseUrl:false</Options>
</OpenApiReference>
```

Versions match `PlainNetCoreMVC` so the two stacks don't drift onto different NSwag majors.

`NZWalks.MVC/OpenAPIs/nzwalks.v1.json` is captured once and committed. Refreshing is manual, because the API serves `/openapi/v1.json` in Development only:

```bash
dotnet run --project ./NZWalks.API
curl -sk "https://localhost:7223/openapi/v1.json" -o NZWalks.MVC/OpenAPIs/nzwalks.v1.json
```

Generated method names derive from operationIds. `RegionsController` overloads `GetRegion` for both "all" and "by id", so expect NSwag to disambiguate with numeric suffixes in the `RegionsGET2Async` style. Read the actual names off the generated file; do not guess them.

### Registration

```csharp
builder.Services.AddHttpClient<INZWalksApiClient, NZWalksApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["NZWalksApi:BaseUrl"]
        ?? throw new InvalidOperationException("Configuration value 'NZWalksApi:BaseUrl' not found.")));
```

Failing fast on missing config matches `PlainNetCoreMVC`. `NZWalksApi:BaseUrl` is `https://localhost:7223/` in `appsettings.json` and `appsettings.Development.json`; the trailing slash matters because the generated client appends relative paths.

### Regions CRUD

`Controllers/RegionsController.cs` — `Index`, `Details`, `Create` (GET/POST), `Edit` (GET/POST), `Delete`, `DeleteConfirmed`. Views under `Views/Regions/`, following the scaffold's Bootstrap layout.

Generated `RegionDto` (`Id`, `Code`, `Name`, `RegionImageUrl`) is used directly for display. Input needs a separate type, because generated DTOs cannot hold an `IFormFile`:

```csharp
public class RegionFormViewModel
{
    [Required] public string Code { get; set; } = default!;
    [Required] public string Name { get; set; } = default!;
    public IFormFile? Image { get; set; }
    public string? ExistingImageUrl { get; set; }   // Edit only, for preview
}
```

On POST the upload is adapted to NSwag's file type:

```csharp
FileParameter? file = vm.Image is null or { Length: 0 }
    ? null
    : new FileParameter(vm.Image.OpenReadStream(), vm.Image.FileName, vm.Image.ContentType);
```

### Image rendering

```csharp
public sealed class NZWalksApiOptions { public string BaseUrl { get; set; } = default!; }
```

Bound from the `NZWalksApi` section and `@inject`ed into views, so image sources render as `{BaseUrl}{RegionImageUrl}` with the leading slash de-duplicated. Without this, every region image 404s against the MVC host.

### Error handling

Each action wraps its client call. The method name below is illustrative — use whatever NSwag actually generates:

```csharp
try
{
    var regions = await _api.RegionsAllAsync(ct);
    return View(regions);
}
catch (Exception ex) when (ex is ApiException or HttpRequestException)
{
    _logger.LogError(ex, "NZWalks API call failed");
    ViewBag.ApiError = "Could not reach the NZWalks API.";
    return View(Array.Empty<RegionDto>());
}
```

Write actions re-render the form with `ViewBag.ApiError` and preserve user input. Explicitly out of scope: Polly retries, and mapping the API's 404 or its 409 "Region is in use because walks reference it" to distinct UI. A 409 surfaces as the generic message.

## Risks

1. **Multipart codegen (highest).** It is unverified whether .NET 10's OpenAPI generator emits the `[FromForm]` + `IFormFile` Region endpoints as a multipart `requestBody` with a binary-format property. If it does not, NSwag produces no `FileParameter` and create/edit-with-image needs a hand-written `HttpClient` fallback for those two calls. **Resolve by inspecting the captured JSON before building the UI.**
2. **Stale contract.** The committed document can drift from the running API. Accepted as the cost of the chosen approach; the refresh command above is the mitigation.
3. **Dev certificate.** Server-to-server HTTPS to `localhost:7223` requires a trusted dev cert (`dotnet dev-certs https --trust`).
4. **Generated-name churn.** Changing the API's action names changes generated client method names and breaks the MVC build. Expected and acceptable.

## Verification

Manual, with both hosts running:

```bash
dotnet run --project ./NZWalks.API     # 7223;5062
dotnet run --project ./NZWalks.MVC     # 7000;5114
```

1. `dotnet build ./NZWalks.MVC` succeeds and the generated client compiles.
2. `/Regions` lists regions from the API; images render rather than 404.
3. Create with and without an image; confirm the new region appears and the file lands in the API's `wwwroot/images/regions/`.
4. Edit an existing region, both changing and keeping the image.
5. Delete a region with no walks (succeeds) and one with walks (API returns 409 → generic error message shown).
6. Stop the API and reload `/Regions`: the friendly error renders instead of an exception page.

## Out of scope

Authentication UI; Walks and Difficulties pages; retry policies; typed 404/409 handling; a test project; any change to `NZWalks.API`.
