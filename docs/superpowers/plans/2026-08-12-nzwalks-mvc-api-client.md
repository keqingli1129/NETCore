# NZWalks.MVC API Client Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `NZWalks.MVC` a build-time-generated typed client for `NZWalks.API` plus a full Regions CRUD UI, including the multipart image upload.

**Architecture:** An `OpenApiReference` in the csproj runs NSwag at build time against a committed OpenAPI document, emitting `INZWalksApiClient` into `obj/` (never committed). A thin hand-written `IRegionsApi` facade wraps that generated client with stable names, so generated-name churn touches exactly one file. `RegionsController` depends only on the facade and renders a friendly in-page error when the API is unreachable.

**Tech Stack:** .NET 10, ASP.NET Core MVC, NSwag 14.4.0 (`NSwag.ApiDescription.Client` + `Microsoft.Extensions.ApiDescription.Client`), Newtonsoft.Json 13.0.3.

**Spec:** `docs/superpowers/specs/2026-08-12-nzwalks-mvc-api-client-design.md`

## Global Constraints

- Target framework `net10.0`; nullable and implicit usings enabled. Do not change these.
- Package versions must match `PlainNetCoreMVC` so the two stacks don't drift onto different NSwag majors: `NSwag.ApiDescription.Client` **14.4.0**, `Microsoft.Extensions.ApiDescription.Client` **9.0.0**, `Newtonsoft.Json` **13.0.3**.
- NSwag options are exactly `/GenerateClientInterfaces:true /UseBaseUrl:false`. `UseBaseUrl:false` is required — the client must take its address from `HttpClient.BaseAddress`.
- **Never commit generated client code.** It is emitted into `obj/`. The only committed artifact is `NZWalks.MVC/OpenAPIs/nzwalks.v1.json`.
- Config key is `NZWalksApi:BaseUrl`, value `https://localhost:7223/`. The trailing slash is required — the generated client appends relative paths.
- No authentication. No Polly/retry. No typed 404/409 mapping. No changes to any file under `NZWalks.API/`.
- **No test project** — this was an explicit spec decision (`NZWalks.API.Tests` is an empty placeholder; PlainNetCore has none). Each task's verification is `dotnet build` plus a stated runtime check against a running API. This deliberately replaces the usual TDD cycle; do not add a test project without asking.
- Ports: `NZWalks.API` `https://localhost:7223`, `NZWalks.MVC` `https://localhost:7000`.
- `dotnet dev-certs https --trust` must have been run, or server-to-server HTTPS calls fail.
- Work on branch `feature/nzwalks-mvc-api-client` (user's explicit choice). `bin/`, `obj/` and `*.user` are already gitignored — never force-add them.
- **Stage only the files each task names.** The working tree carries unrelated pending changes (`NETCore.sln` modified, `WebApplication1/` deleted, `.claude/settings.local.json` modified) that must stay uncommitted. Never run `git add -A`, `git add .`, or `git commit -a`.

---

## File Structure

| File | Responsibility |
|---|---|
| `NZWalks.MVC/OpenAPIs/nzwalks.v1.json` | Committed API contract; codegen input |
| `NZWalks.MVC/NZWalks.MVC.csproj` | Packages + `OpenApiReference` |
| `NZWalks.MVC/ApiClients/IRegionsApi.cs` | Stable facade interface over the generated client |
| `NZWalks.MVC/ApiClients/RegionsApi.cs` | Only file that names generated methods |
| `NZWalks.MVC/Models/NZWalksApiOptions.cs` | Base URL + resolving API-relative image paths |
| `NZWalks.MVC/Models/RegionFormViewModel.cs` | Create/Edit form input incl. `IFormFile` |
| `NZWalks.MVC/Controllers/RegionsController.cs` | Thin actions, error catching |
| `NZWalks.MVC/Views/Regions/*.cshtml` | Index, Details, Create, Edit, Delete |
| `NZWalks.MVC/Views/Shared/_ApiError.cshtml` | Error banner partial (shared by all five views) |
| `NZWalks.MVC/Views/_ViewImports.cshtml` | Usings for generated + model namespaces |
| `NZWalks.MVC/Views/Shared/_Layout.cshtml` | Nav link |
| `NZWalks.MVC/Program.cs` | DI registration |
| `NZWalks.MVC/appsettings*.json` | `NZWalksApi:BaseUrl` |
| `NZWalks.MVC/CLAUDE.md` | Update once the client exists |

---

### Task 1: Capture the API contract and resolve the multipart risk

This is a **decision gate**. The spec's top risk is whether .NET 10's OpenAPI output describes the Region image endpoints as multipart with a binary property. If it does not, stop and report before writing any UI.

**Files:**
- Create: `NZWalks.MVC/OpenAPIs/nzwalks.v1.json`

**Interfaces:**
- Consumes: nothing.
- Produces: the committed contract that Task 2's codegen reads.

- [ ] **Step 1: Start the API**

```bash
dotnet run --project ./NZWalks.API
```

Leave it running. It listens on `https://localhost:7223`. It needs SQL Server reachable via the `NZWalksConnectionString` in `NZWalks.API/appsettings.json`; the OpenAPI document is served even if the database is down, so a DB failure does not block this task.

- [ ] **Step 2: Capture the document**

```bash
mkdir -p NZWalks.MVC/OpenAPIs
curl -sk "https://localhost:7223/openapi/v1.json" -o NZWalks.MVC/OpenAPIs/nzwalks.v1.json
```

- [ ] **Step 3: Verify it is a real OpenAPI document**

```bash
python -c "import json; d=json.load(open('NZWalks.MVC/OpenAPIs/nzwalks.v1.json')); print(d['openapi']); print(sorted(d['paths'].keys()))"
```

Expected: a version string, and paths including `/api/Regions`, `/api/Regions/{id}`, `/api/Walks`, `/api/Difficulties`, `/api/Auth/Login`.

- [ ] **Step 4: Inspect the Regions POST request body — THE GATE**

```bash
python -c "
import json
d = json.load(open('NZWalks.MVC/OpenAPIs/nzwalks.v1.json'))
rb = d['paths']['/api/Regions']['post'].get('requestBody', {})
print('content types:', list(rb.get('content', {}).keys()))
print(json.dumps(rb, indent=2)[:1500])
"
```

Expected (the good case): `content types: ['multipart/form-data']`, and inside its schema a `properties` block with `code`, `name`, and an `image` property of `"type": "string", "format": "binary"`.

**If `image` is missing, or has no `"format": "binary"`, or the content type is not `multipart/form-data`:** STOP. Do not proceed to Task 5/6. Report the actual JSON shape and note that Regions create/edit-with-image needs a hand-written `HttpClient` call instead of the generated method. Tasks 1-4, 7 and 8 remain valid.

- [ ] **Step 5: Commit the scaffold and the contract**

`NZWalks.MVC/` is entirely untracked, so this first commit adds the project. Confirm nothing ignored slips in:

```bash
git add NZWalks.MVC
git status --short NZWalks.MVC | grep -E "obj/|bin/|\.user" && echo "PROBLEM: ignored files staged" || echo "clean"
git commit -m "Add NZWalks.MVC scaffold and captured NZWalks.API contract"
```

---

### Task 2: Wire codegen and add the Regions facade

**Files:**
- Modify: `NZWalks.MVC/NZWalks.MVC.csproj`
- Create: `NZWalks.MVC/ApiClients/IRegionsApi.cs`
- Create: `NZWalks.MVC/ApiClients/RegionsApi.cs`

**Interfaces:**
- Consumes: `NZWalks.MVC/OpenAPIs/nzwalks.v1.json` from Task 1.
- Produces: `NZWalks.MVC.ApiClients.IRegionsApi` with `GetAllAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` (exact signatures in Step 3). Also the generated `NZWalks.MVC.ApiClients.INZWalksApiClient`, `RegionDto`, `FileParameter` and `ApiException` types. Tasks 3-8 use only `IRegionsApi`, `RegionDto`, `FileParameter` and `ApiException`.

- [ ] **Step 1: Replace the csproj with the codegen wiring**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <OpenApiReference Include="OpenAPIs\nzwalks.v1.json"
                      ClassName="NZWalksApiClient"
                      Namespace="NZWalks.MVC.ApiClients">
      <SourceUri>https://localhost:7223/openapi/v1.json</SourceUri>
      <Options>/GenerateClientInterfaces:true /UseBaseUrl:false</Options>
    </OpenApiReference>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.ApiDescription.Client" Version="9.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="NSwag.ApiDescription.Client" Version="14.4.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Build and confirm the client is generated**

```bash
dotnet build ./NZWalks.MVC
```

Expected: build succeeds. Then locate the generated file and read the interface:

```bash
find NZWalks.MVC/obj -name "*NZWalksApiClient*.cs" | head
grep -n "Task<" $(find NZWalks.MVC/obj -name "*NZWalksApiClient*.cs" | head -1) | head -20
```

Record the exact Regions method names. They will look like `RegionsAllAsync` / `RegionsGETAsync`, `RegionsPOSTAsync`, `RegionsGET2Async`, `RegionsPUTAsync`, `RegionsDELETEAsync` — NSwag appends numeric suffixes because `RegionsController` overloads `GetRegion`. **Use the names you just read, not these guesses.** Note also whether GET-all returns `ICollection<RegionDto>` or `IEnumerable<RegionDto>`, and the exact parameter order on the POST/PUT methods.

- [ ] **Step 3: Write the facade interface**

Create `NZWalks.MVC/ApiClients/IRegionsApi.cs`:

```csharp
namespace NZWalks.MVC.ApiClients;

/// <summary>
/// Stable wrapper over the NSwag-generated client. Generated method names change
/// whenever NZWalks.API renames an action, so confine that churn to RegionsApi.
/// </summary>
public interface IRegionsApi
{
    Task<IReadOnlyList<RegionDto>> GetAllAsync(CancellationToken ct = default);

    Task<RegionDto> GetAsync(int id, CancellationToken ct = default);

    Task<RegionDto> CreateAsync(string code, string name, FileParameter? image, CancellationToken ct = default);

    Task<RegionDto> UpdateAsync(int id, string code, string name, FileParameter? image, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

- [ ] **Step 4: Write the facade implementation**

Create `NZWalks.MVC/ApiClients/RegionsApi.cs`. Substitute the real generated method names recorded in Step 2 for the `Regions*Async` calls below:

```csharp
namespace NZWalks.MVC.ApiClients;

public sealed class RegionsApi : IRegionsApi
{
    private readonly INZWalksApiClient _client;

    public RegionsApi(INZWalksApiClient client) => _client = client;

    public async Task<IReadOnlyList<RegionDto>> GetAllAsync(CancellationToken ct = default)
        => (await _client.RegionsAllAsync(ct)).ToList();

    public Task<RegionDto> GetAsync(int id, CancellationToken ct = default)
        => _client.RegionsGET2Async(id, ct);

    public Task<RegionDto> CreateAsync(string code, string name, FileParameter? image, CancellationToken ct = default)
        => _client.RegionsPOSTAsync(code, name, image, ct);

    public Task<RegionDto> UpdateAsync(int id, string code, string name, FileParameter? image, CancellationToken ct = default)
        => _client.RegionsPUTAsync(id, code, name, image, ct);

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => _client.RegionsDELETEAsync(id, ct);
}
```

If a generated signature differs (for example PUT takes the id last, or DELETE returns `RegionDto`), adapt the body here — the interface in Step 3 must not change.

- [ ] **Step 5: Build**

```bash
dotnet build ./NZWalks.MVC
```

Expected: PASS. Compile errors here mean a generated name was mistyped; re-read the generated file.

- [ ] **Step 6: Commit**

```bash
git add NZWalks.MVC/NZWalks.MVC.csproj NZWalks.MVC/ApiClients
git commit -m "Generate NZWalks.API client at build time and add Regions facade"
```

---

### Task 3: Configuration and DI registration

**Files:**
- Create: `NZWalks.MVC/Models/NZWalksApiOptions.cs`
- Modify: `NZWalks.MVC/Program.cs`
- Modify: `NZWalks.MVC/appsettings.json`
- Modify: `NZWalks.MVC/appsettings.Development.json`

**Interfaces:**
- Consumes: `INZWalksApiClient`, `IRegionsApi`, `RegionsApi` from Task 2.
- Produces: `NZWalks.MVC.Models.NZWalksApiOptions` with `string BaseUrl` and `string? ResolveUrl(string?)`, registered as a singleton. `IRegionsApi` resolvable from DI.

- [ ] **Step 1: Write the options type**

Create `NZWalks.MVC/Models/NZWalksApiOptions.cs`:

```csharp
namespace NZWalks.MVC.Models;

public sealed class NZWalksApiOptions
{
    public string BaseUrl { get; set; } = default!;

    /// <summary>
    /// Turns an API-relative path (e.g. "/images/regions/abc.png") into an absolute
    /// URL. NZWalks.API saves uploads into its own wwwroot and returns host-relative
    /// paths, which would 404 against this app's host if rendered verbatim.
    /// Already-absolute URLs pass through untouched.
    /// </summary>
    public string? ResolveUrl(string? apiRelativePath)
    {
        if (string.IsNullOrWhiteSpace(apiRelativePath))
        {
            return null;
        }

        // Seeded regions hold absolute URLs (https://example.com/...); uploads return
        // host-relative paths. Prepending the base URL to an absolute URL yields
        // "https://localhost:7223/https://example.com/..." and a broken image.
        if (Uri.TryCreate(apiRelativePath, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return apiRelativePath;
        }

        return $"{BaseUrl.TrimEnd('/')}/{apiRelativePath.TrimStart('/')}";
    }
}
```

**Amended 2026-08-12 after Task 4 runtime verification.** The original one-line body prepended the base URL unconditionally, which mangled the five seeded regions' absolute `RegionImageUrl` values and broke every image on the list and details pages. Human ruling: fix `ResolveUrl` rather than the seed data or the call sites.

- [ ] **Step 2: Add config to both appsettings files**

In `NZWalks.MVC/appsettings.json`, add a sibling of `"AllowedHosts"`:

```json
  "NZWalksApi": {
    "BaseUrl": "https://localhost:7223/"
  }
```

Add the same section to `NZWalks.MVC/appsettings.Development.json`.

- [ ] **Step 3: Register in Program.cs**

Add these usings at the top of `NZWalks.MVC/Program.cs`, above `namespace NZWalks.MVC`:

```csharp
using NZWalks.MVC.ApiClients;
using NZWalks.MVC.Models;
```

Then replace the line `builder.Services.AddControllersWithViews();` with:

```csharp
            builder.Services.AddControllersWithViews();

            var apiOptions = builder.Configuration.GetSection("NZWalksApi").Get<NZWalksApiOptions>();
            if (string.IsNullOrWhiteSpace(apiOptions?.BaseUrl))
            {
                throw new InvalidOperationException("Configuration value 'NZWalksApi:BaseUrl' not found.");
            }

            builder.Services.AddSingleton(apiOptions);
            builder.Services.AddHttpClient<INZWalksApiClient, NZWalksApiClient>(client =>
                client.BaseAddress = new Uri(apiOptions.BaseUrl));
            builder.Services.AddScoped<IRegionsApi, RegionsApi>();
```

Keep the surrounding indentation — `Program.Main` is inside a namespace and class, so this code sits at 12 spaces.

- [ ] **Step 4: Verify the app starts**

```bash
dotnet build ./NZWalks.MVC
dotnet run --project ./NZWalks.MVC
```

Expected: build PASS, app starts and serves `https://localhost:7000` (the existing Home page still renders). Stop it.

- [ ] **Step 5: Verify fail-fast on missing config**

Temporarily rename the `NZWalksApi` section in `appsettings.json` to `NZWalksApiX`, then:

```bash
dotnet run --project ./NZWalks.MVC
```

Expected: startup throws `InvalidOperationException: Configuration value 'NZWalksApi:BaseUrl' not found.` Restore the section name and confirm it starts again.

- [ ] **Step 6: Commit**

```bash
git add NZWalks.MVC/Program.cs NZWalks.MVC/Models NZWalks.MVC/appsettings.json NZWalks.MVC/appsettings.Development.json
git commit -m "Register NZWalks.API typed client and base URL options"
```

---

### Task 4: Regions list and details

**Files:**
- Create: `NZWalks.MVC/Controllers/RegionsController.cs`
- Create: `NZWalks.MVC/Views/Regions/Index.cshtml`
- Create: `NZWalks.MVC/Views/Regions/Details.cshtml`
- Create: `NZWalks.MVC/Views/Shared/_ApiError.cshtml`
- Modify: `NZWalks.MVC/Views/_ViewImports.cshtml`
- Modify: `NZWalks.MVC/Views/Shared/_Layout.cshtml`

**Interfaces:**
- Consumes: `IRegionsApi`, `RegionDto`, `ApiException` (Task 2); `NZWalksApiOptions` (Task 3).
- Produces: `RegionsController` with `Index()` and `Details(int id)`. Tasks 5-7 add actions to this same controller and reuse the `_ApiError` partial.

- [ ] **Step 1: Add namespaces to _ViewImports.cshtml**

Append to `NZWalks.MVC/Views/_ViewImports.cshtml`:

```cshtml
@using NZWalks.MVC.ApiClients
@using NZWalks.MVC.Models
```

- [ ] **Step 2: Create the error partial**

Create `NZWalks.MVC/Views/Shared/_ApiError.cshtml`:

```cshtml
@if (ViewData["ApiError"] is string apiError && !string.IsNullOrWhiteSpace(apiError))
{
    <div class="alert alert-danger" role="alert">@apiError</div>
}
```

- [ ] **Step 3: Write the controller**

Create `NZWalks.MVC/Controllers/RegionsController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using NZWalks.MVC.ApiClients;

namespace NZWalks.MVC.Controllers;

public class RegionsController : Controller
{
    private const string UnreachableMessage = "Could not reach the NZWalks API.";

    private readonly IRegionsApi _api;
    private readonly ILogger<RegionsController> _logger;

    public RegionsController(IRegionsApi api, ILogger<RegionsController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var regions = await _api.GetAllAsync(ct);
            return View(regions);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            _logger.LogError(ex, "NZWalks API call failed listing regions");
            ViewData["ApiError"] = UnreachableMessage;
            return View(Array.Empty<RegionDto>());
        }
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        try
        {
            return View(await _api.GetAsync(id, ct));
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            _logger.LogError(ex, "NZWalks API call failed loading region {RegionId}", id);
            ViewData["ApiError"] = UnreachableMessage;
            return View(model: null);
        }
    }
}
```

The 404 catch is not the "typed 404/409 mapping" the spec excluded — without it a missing id renders a broken page instead of a 404. The excluded work was surfacing the Delete endpoint's 409 as distinct UI.

- [ ] **Step 4: Write the Index view**

Create `NZWalks.MVC/Views/Regions/Index.cshtml`:

```cshtml
@model IReadOnlyList<RegionDto>
@inject NZWalksApiOptions ApiOptions
@{
    ViewData["Title"] = "Regions";
}

<h1>Regions</h1>
<partial name="_ApiError" />

<p><a class="btn btn-primary" asp-action="Create">Create New</a></p>

@if (!Model.Any())
{
    <p>No regions to show.</p>
}
else
{
    <table class="table">
        <thead>
            <tr><th>Code</th><th>Name</th><th>Image</th><th></th></tr>
        </thead>
        <tbody>
        @foreach (var region in Model)
        {
            <tr>
                <td>@region.Code</td>
                <td>@region.Name</td>
                <td>
                    @{ var imageUrl = ApiOptions.ResolveUrl(region.RegionImageUrl); }
                    @if (imageUrl is not null)
                    {
                        <img src="@imageUrl" alt="@region.Name" style="max-height:60px" />
                    }
                </td>
                <td>
                    <a asp-action="Details" asp-route-id="@region.Id">Details</a> |
                    <a asp-action="Edit" asp-route-id="@region.Id">Edit</a> |
                    <a asp-action="Delete" asp-route-id="@region.Id">Delete</a>
                </td>
            </tr>
        }
        </tbody>
    </table>
}
```

The `Create`, `Edit` and `Delete` links target actions added in Tasks 5-7. They render fine before those exist but 404 when clicked; that is expected until Task 7 lands.

- [ ] **Step 5: Write the Details view**

Create `NZWalks.MVC/Views/Regions/Details.cshtml`:

```cshtml
@model RegionDto
@inject NZWalksApiOptions ApiOptions
@{
    ViewData["Title"] = "Region details";
}

<h1>Region details</h1>
<partial name="_ApiError" />

@if (Model is not null)
{
    <dl class="row">
        <dt class="col-sm-2">Code</dt><dd class="col-sm-10">@Model.Code</dd>
        <dt class="col-sm-2">Name</dt><dd class="col-sm-10">@Model.Name</dd>
    </dl>

    @{ var imageUrl = ApiOptions.ResolveUrl(Model.RegionImageUrl); }
    @if (imageUrl is not null)
    {
        <img src="@imageUrl" alt="@Model.Name" class="img-fluid" style="max-height:300px" />
    }
}

<p class="mt-3"><a asp-action="Index">Back to list</a></p>
```

- [ ] **Step 6: Add the nav link**

In `NZWalks.MVC/Views/Shared/_Layout.cshtml`, inside `<ul class="navbar-nav flex-grow-1">` after the Home `<li>`, add:

```cshtml
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-controller="Regions" asp-action="Index">Regions</a>
                        </li>
```

- [ ] **Step 7: Verify against the running API**

Start both hosts, then:

```bash
dotnet run --project ./NZWalks.API
dotnet run --project ./NZWalks.MVC
curl -sk "https://localhost:7000/Regions" | grep -c "<tr>"
```

Expected: build PASS; the page lists regions (row count matches `curl -sk https://localhost:7223/api/Regions` output); images load rather than 404 (check the browser network tab or `curl -skI` the rendered image URL — it must point at port 7223, not 7000).

- [ ] **Step 8: Verify the API-down path**

Stop `NZWalks.API`, reload `https://localhost:7000/Regions`.

Expected: the red "Could not reach the NZWalks API." banner with "No regions to show." — not an exception page. Restart the API.

- [ ] **Step 9: Commit**

```bash
git add NZWalks.MVC/Controllers NZWalks.MVC/Views
git commit -m "Add Regions list and details pages backed by the API client"
```

---

### Task 5: Regions create with image upload

**Gated on Task 1 Step 4 passing.** If the contract has no binary `image` property, stop and report.

**Files:**
- Create: `NZWalks.MVC/Models/RegionFormViewModel.cs`
- Create: `NZWalks.MVC/Views/Regions/Create.cshtml`
- Modify: `NZWalks.MVC/Controllers/RegionsController.cs`

**Interfaces:**
- Consumes: `IRegionsApi.CreateAsync`, `FileParameter` (Task 2).
- Produces: `NZWalks.MVC.Models.RegionFormViewModel` with `Code`, `Name`, `IFormFile? Image`, `string? ExistingImageUrl`; and a `ToFileParameter(IFormFile?)` helper on the controller reused by Task 6.

- [ ] **Step 1: Write the form view model**

Create `NZWalks.MVC/Models/RegionFormViewModel.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace NZWalks.MVC.Models;

public class RegionFormViewModel
{
    [Required]
    [StringLength(3, MinimumLength = 2, ErrorMessage = "Code must be 2 or 3 characters.")]
    public string Code { get; set; } = default!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = default!;

    [Display(Name = "Image")]
    public IFormFile? Image { get; set; }

    /// <summary>Populated on Edit so the current image can be previewed.</summary>
    public string? ExistingImageUrl { get; set; }
}
```

- [ ] **Step 2: Add the create actions**

In `RegionsController`, add these usings at the top:

```csharp
using NZWalks.MVC.Models;
```

Then add to the class:

```csharp
    [HttpGet]
    public IActionResult Create() => View(new RegionFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegionFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _api.CreateAsync(form.Code, form.Name, ToFileParameter(form.Image), ct);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            _logger.LogError(ex, "NZWalks API call failed creating region");
            ViewData["ApiError"] = UnreachableMessage;
            return View(form);
        }
    }

    private static FileParameter? ToFileParameter(IFormFile? file)
        => file is null || file.Length == 0
            ? null
            : new FileParameter(file.OpenReadStream(), file.FileName, file.ContentType);
```

`OpenReadStream()` is not disposed here deliberately — the generated client reads it while building the multipart body, and the request completes before the action returns.

- [ ] **Step 3: Write the Create view**

Create `NZWalks.MVC/Views/Regions/Create.cshtml`:

```cshtml
@model RegionFormViewModel
@{
    ViewData["Title"] = "Create region";
}

<h1>Create region</h1>
<partial name="_ApiError" />

<form asp-action="Create" enctype="multipart/form-data" method="post">
    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
    <div class="mb-3">
        <label asp-for="Code" class="form-label"></label>
        <input asp-for="Code" class="form-control" />
        <span asp-validation-for="Code" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Name" class="form-label"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Image" class="form-label"></label>
        <input asp-for="Image" type="file" class="form-control" accept="image/*" />
    </div>
    <button type="submit" class="btn btn-primary">Create</button>
    <a asp-action="Index" class="btn btn-secondary">Cancel</a>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

`enctype="multipart/form-data"` is required or the file never reaches the server.

- [ ] **Step 4: Verify create without an image**

Build, run both hosts, open `https://localhost:7000/Regions/Create`, submit Code `TST` / Name `Test Region` with no file.

Expected: redirect to the list, new row present, no image cell content.

- [ ] **Step 5: Verify create with an image**

Submit Code `IMG` / Name `Image Region` with a small PNG or JPEG.

Expected: redirect to the list; the thumbnail renders; the file exists under `NZWalks.API/wwwroot/images/regions/`:

```bash
ls NZWalks.API/wwwroot/images/regions/
```

- [ ] **Step 6: Verify validation blocks bad input**

Submit an empty form.

Expected: the page re-renders with "The Code field is required." and no API call is made (nothing new in the API console log).

- [ ] **Step 7: Commit**

```bash
git add NZWalks.MVC/Controllers/RegionsController.cs NZWalks.MVC/Models/RegionFormViewModel.cs NZWalks.MVC/Views/Regions/Create.cshtml
git commit -m "Add Regions create page with multipart image upload"
```

---

### Task 6: Regions edit

**Gated on Task 1 Step 4 passing**, same as Task 5.

**Files:**
- Create: `NZWalks.MVC/Views/Regions/Edit.cshtml`
- Modify: `NZWalks.MVC/Controllers/RegionsController.cs`

**Interfaces:**
- Consumes: `IRegionsApi.GetAsync`, `IRegionsApi.UpdateAsync`, `ToFileParameter` (Task 5), `RegionFormViewModel` (Task 5), `NZWalksApiOptions.ResolveUrl` (Task 3).
- Produces: `Edit(int id)` GET and `Edit(int id, RegionFormViewModel form)` POST.

- [ ] **Step 1: Add the edit actions**

Add to `RegionsController`:

```csharp
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        try
        {
            var region = await _api.GetAsync(id, ct);
            return View(new RegionFormViewModel
            {
                Code = region.Code,
                Name = region.Name,
                ExistingImageUrl = region.RegionImageUrl
            });
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            _logger.LogError(ex, "NZWalks API call failed loading region {RegionId} for edit", id);
            ViewData["ApiError"] = UnreachableMessage;
            return View(new RegionFormViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RegionFormViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _api.UpdateAsync(id, form.Code, form.Name, ToFileParameter(form.Image), ct);
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            _logger.LogError(ex, "NZWalks API call failed updating region {RegionId}", id);
            ViewData["ApiError"] = UnreachableMessage;
            return View(form);
        }
    }
```

Note a real API behaviour to surface in Step 4: `RegionsController.PutRegion` on the API sets `RegionImageUrl` from the uploaded file unconditionally, so **submitting Edit with no file clears the existing image**. Do not try to fix this in the MVC app — it is API behaviour and `NZWalks.API` is out of scope. Record it in Task 8's notes.

- [ ] **Step 2: Write the Edit view**

Create `NZWalks.MVC/Views/Regions/Edit.cshtml`:

```cshtml
@model RegionFormViewModel
@inject NZWalksApiOptions ApiOptions
@{
    ViewData["Title"] = "Edit region";
}

<h1>Edit region</h1>
<partial name="_ApiError" />

@{ var existingImageUrl = ApiOptions.ResolveUrl(Model.ExistingImageUrl); }
@if (existingImageUrl is not null)
{
    <p><img src="@existingImageUrl" alt="@Model.Name" style="max-height:120px" /></p>
    <p class="text-muted">Submitting without choosing a file clears the current image.</p>
}

<form asp-action="Edit" enctype="multipart/form-data" method="post">
    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
    <input type="hidden" asp-for="ExistingImageUrl" />
    <div class="mb-3">
        <label asp-for="Code" class="form-label"></label>
        <input asp-for="Code" class="form-control" />
        <span asp-validation-for="Code" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Name" class="form-label"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Image" class="form-label"></label>
        <input asp-for="Image" type="file" class="form-control" accept="image/*" />
    </div>
    <button type="submit" class="btn btn-primary">Save</button>
    <a asp-action="Index" class="btn btn-secondary">Cancel</a>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

The `id` route value comes from the URL, so the form does not need a hidden id field.

- [ ] **Step 3: Verify editing text fields**

Build, run both hosts, edit the `TST` region's Name, submit with no file.

Expected: redirect to the list, new Name shown.

- [ ] **Step 4: Verify image replacement and the clearing behaviour**

Edit the `IMG` region and upload a different image. Expected: the new thumbnail renders. Then edit it again submitting no file. Expected: the image disappears from the list — confirming the API-side behaviour noted in Step 1.

- [ ] **Step 5: Verify a missing id 404s**

Visit `https://localhost:7000/Regions/Edit/999999`.

Expected: HTTP 404, not an exception page.

- [ ] **Step 6: Commit**

```bash
git add NZWalks.MVC/Controllers/RegionsController.cs NZWalks.MVC/Views/Regions/Edit.cshtml
git commit -m "Add Regions edit page"
```

---

### Task 7: Regions delete

**Files:**
- Create: `NZWalks.MVC/Views/Regions/Delete.cshtml`
- Modify: `NZWalks.MVC/Controllers/RegionsController.cs`

**Interfaces:**
- Consumes: `IRegionsApi.GetAsync`, `IRegionsApi.DeleteAsync` (Task 2).
- Produces: `Delete(int id)` GET and `DeleteConfirmed(int id)` POST.

- [ ] **Step 1: Add the delete actions**

Add to `RegionsController`:

```csharp
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            return View(await _api.GetAsync(id, ct));
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            _logger.LogError(ex, "NZWalks API call failed loading region {RegionId} for delete", id);
            ViewData["ApiError"] = UnreachableMessage;
            return View(model: null);
        }
    }

    [HttpPost, ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        try
        {
            await _api.DeleteAsync(id, ct);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            _logger.LogError(ex, "NZWalks API call failed deleting region {RegionId}", id);
            ViewData["ApiError"] = UnreachableMessage;
            try
            {
                return View(await _api.GetAsync(id, ct));
            }
            catch (Exception reload) when (reload is ApiException or HttpRequestException)
            {
                return View(model: null);
            }
        }
    }
```

Per the spec, the API's 409 ("Region is in use because walks reference it") surfaces as the same generic message — no distinct UI.

- [ ] **Step 2: Write the Delete view**

Create `NZWalks.MVC/Views/Regions/Delete.cshtml`:

```cshtml
@model RegionDto
@inject NZWalksApiOptions ApiOptions
@{
    ViewData["Title"] = "Delete region";
}

<h1>Delete region</h1>
<partial name="_ApiError" />

@if (Model is not null)
{
    <p>Are you sure you want to delete this region?</p>
    <dl class="row">
        <dt class="col-sm-2">Code</dt><dd class="col-sm-10">@Model.Code</dd>
        <dt class="col-sm-2">Name</dt><dd class="col-sm-10">@Model.Name</dd>
    </dl>

    @{ var imageUrl = ApiOptions.ResolveUrl(Model.RegionImageUrl); }
    @if (imageUrl is not null)
    {
        <img src="@imageUrl" alt="@Model.Name" style="max-height:120px" />
    }

    <form asp-action="Delete" asp-route-id="@Model.Id" method="post" class="mt-3">
        <button type="submit" class="btn btn-danger">Delete</button>
        <a asp-action="Index" class="btn btn-secondary">Cancel</a>
    </form>
}
else
{
    <p><a asp-action="Index">Back to list</a></p>
}
```

- [ ] **Step 3: Verify deleting an unreferenced region**

Build, run both hosts, delete the `TST` region created in Task 5.

Expected: redirect to the list, row gone.

- [ ] **Step 4: Verify the 409 path**

Find a region referenced by a walk:

```bash
curl -sk "https://localhost:7223/api/Walks" | python -c "import json,sys; print({w['regionId'] for w in json.load(sys.stdin)})"
```

Attempt to delete one of those regions through the UI.

Expected: the delete page re-renders with "Could not reach the NZWalks API." and the region still exists. The message is knowingly inaccurate for this case — it is the accepted spec trade-off.

- [ ] **Step 5: Commit**

```bash
git add NZWalks.MVC/Controllers/RegionsController.cs NZWalks.MVC/Views/Regions/Delete.cshtml
git commit -m "Add Regions delete page"
```

---

### Task 8: Update project docs and verify end to end

**Files:**
- Modify: `NZWalks.MVC/CLAUDE.md`

**Interfaces:**
- Consumes: everything from Tasks 1-7.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Full clean build**

```bash
dotnet build ./NETCore.sln -c Release
```

Expected: the whole solution builds. Codegen runs from the committed contract with no network access needed.

- [ ] **Step 2: Confirm no generated code was committed**

```bash
git ls-files NZWalks.MVC | grep -iE "NZWalksApiClient|obj/|bin/" && echo "PROBLEM: generated or build output tracked" || echo "clean"
```

Expected: `clean`. Only `OpenAPIs/nzwalks.v1.json`, the csproj, `ApiClients/IRegionsApi.cs`, `ApiClients/RegionsApi.cs`, models, controller and views are tracked.

- [ ] **Step 3: Walk the full verification list from the spec**

With both hosts running, confirm each item in the spec's Verification section: list, create with and without image, edit both ways, delete unreferenced and referenced, and API-down error rendering.

- [ ] **Step 4: Update NZWalks.MVC/CLAUDE.md**

The file currently says this project has "no client for its paired API yet" and "No external NuGet dependencies beyond the SDK". Both are now false. Rewrite the **Project Overview** second paragraph as:

```markdown
This project is the **front end half of the NZWalks stack** inside `NETCore.sln`. It consumes `NZWalks.API` through a typed client generated at build time by NSwag from a committed OpenAPI document, and ships a full Regions CRUD UI. It still has no DbContext and no authentication.
```

Under **Conventions**, replace the "No external NuGet dependencies" bullet with:

```markdown
- Dependencies are limited to the NSwag codegen toolchain (`NSwag.ApiDescription.Client`, `Microsoft.Extensions.ApiDescription.Client`) plus `Newtonsoft.Json`, which generated client code requires at runtime.
- The API client is **generated into `obj/` and never committed** — unlike `PlainNetCoreMVC`, which checks its NSwag output into source control. The only committed artifact is `OpenAPIs/nzwalks.v1.json`.
- Controllers depend on `IRegionsApi` (`ApiClients/`), a thin facade over the generated client. Generated method names change whenever an API action is renamed; keep that churn inside `RegionsApi`.
```

Add a new section after **Related projects**, with the heading `## Refreshing the API contract`, containing:

~~~markdown
Codegen reads the committed `OpenAPIs/nzwalks.v1.json`, so the build never needs the API running. Refresh it after any change to the API's routes or DTOs:

```bash
dotnet run --project ./NZWalks.API
curl -sk "https://localhost:7223/openapi/v1.json" -o NZWalks.MVC/OpenAPIs/nzwalks.v1.json
```

Then rebuild and fix any compile errors in `ApiClients/RegionsApi.cs`, where generated names are referenced.

Known API-side behaviour: `PUT api/Regions/{id}` sets the image URL from the uploaded file unconditionally, so saving an edit without choosing a file clears the region's existing image. That lives in `NZWalks.API`, not here.
~~~

- [ ] **Step 5: Commit**

```bash
git add NZWalks.MVC/CLAUDE.md
git commit -m "Document the generated NZWalks.API client in project guidance"
```

---

## Self-Review Notes

**Spec coverage:** Codegen wiring → Task 2. Registration → Task 3. Regions CRUD → Tasks 4-7. Image rendering → Task 3 (`ResolveUrl`) + Tasks 4/6/7 (views). Error handling → every controller action. Risk 1 (multipart) → Task 1 Step 4 gate. Risk 2 (stale contract) → Task 8 Step 4 refresh docs. Risk 3 (dev cert) → Global Constraints. Risk 4 (name churn) → the `IRegionsApi` facade. Verification list → Task 8 Step 3. No auth, no retry, no 409 UI, no test project → Global Constraints.

**Deviations from the spec, both deliberate:**
1. **`IRegionsApi` facade added.** The spec had controllers calling the generated client directly. Generated names are unknowable until codegen runs, so this confines them to one file and lets Tasks 3-8 be written against fixed signatures.
2. **404 handled explicitly** in Details/Edit/Delete GET. Without it a bad id renders a broken page. The spec's exclusion was about the Delete 409, which remains generic.

**Type consistency:** `IRegionsApi` members (`GetAllAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`) are used with matching signatures in Tasks 4-7. `RegionFormViewModel` properties (`Code`, `Name`, `Image`, `ExistingImageUrl`) match between Tasks 5 and 6. `NZWalksApiOptions.ResolveUrl` is defined in Task 3 and used in Tasks 4, 6, 7. `ViewData["ApiError"]` is the single error key, written by the controller and read by `_ApiError.cshtml`. `ToFileParameter` is defined in Task 5 and reused in Task 6.
