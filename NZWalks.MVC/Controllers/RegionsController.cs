using Microsoft.AspNetCore.Mvc;
using NZWalks.MVC.ApiClients;
using NZWalks.MVC.Models;

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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to render for.
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is ApiException
                                     or HttpRequestException
                                     or OperationCanceledException)
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to render for.
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is ApiException
                                     or HttpRequestException
                                     or OperationCanceledException)
        {
            _logger.LogError(ex, "NZWalks API call failed loading region {RegionId}", id);
            ViewData["ApiError"] = UnreachableMessage;
            return View(model: null);
        }
    }

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

        if (!ImageUploadValidator.IsValid(form.Image))
        {
            ModelState.AddModelError(nameof(form.Image), ImageUploadValidator.ErrorMessage);
            return View(form);
        }

        try
        {
            await _api.CreateAsync(form.Code, form.Name, ToFileParameter(form.Image), ct);
            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to render for.
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is ApiException
                                     or HttpRequestException
                                     or OperationCanceledException)
        {
            _logger.LogError(ex, "NZWalks API call failed creating region");
            ViewData["ApiError"] = UnreachableMessage;
            return View(form);
        }
    }

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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to render for.
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is ApiException
                                     or HttpRequestException
                                     or OperationCanceledException)
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

        if (!ImageUploadValidator.IsValid(form.Image))
        {
            ModelState.AddModelError(nameof(form.Image), ImageUploadValidator.ErrorMessage);
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to render for.
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is ApiException
                                     or HttpRequestException
                                     or OperationCanceledException)
        {
            _logger.LogError(ex, "NZWalks API call failed updating region {RegionId}", id);
            ViewData["ApiError"] = UnreachableMessage;
            return View(form);
        }
    }

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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to render for.
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is ApiException
                                     or HttpRequestException
                                     or OperationCanceledException)
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
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected; there is nobody to render for.
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is ApiException
                                     or HttpRequestException
                                     or OperationCanceledException)
        {
            _logger.LogError(ex, "NZWalks API call failed deleting region {RegionId}", id);
            ViewData["ApiError"] = UnreachableMessage;
            try
            {
                return View(await _api.GetAsync(id, ct));
            }
            catch (Exception reload) when (reload is ApiException
                                             or HttpRequestException
                                             or OperationCanceledException)
            {
                return View(model: null);
            }
        }
    }

    private static FileParameter? ToFileParameter(IFormFile? file)
        => file is null || file.Length == 0
            ? null
            : new FileParameter(file.OpenReadStream(), SanitizeFileName(file.FileName), file.ContentType);

    // The generated client's Regions*Async methods build a
    // MultipartFormDataContent and call content_.Add(content_image_, "Image",
    // fileName ?? "Image"). MultipartFormDataContent.Add validates that name and
    // throws ArgumentException when it contains '"', CR, LF, or is null/empty/
    // whitespace - an exception type that is not ApiException/HttpRequestException/
    // OperationCanceledException, so it would escape every catch filter below and
    // surface as an unhandled 500. Strip any path component (the API only ever
    // reads Path.GetExtension of this value, never the directory) and substitute a
    // safe placeholder - preserving the real extension when it is itself safe -
    // whenever the raw name is unusable.
    private static string SanitizeFileName(string? fileName)
    {
        var candidate = Path.GetFileName(fileName ?? string.Empty);
        if (IsSafeMultipartName(candidate))
        {
            return candidate;
        }

        string extension;
        try
        {
            extension = Path.GetExtension(candidate) ?? string.Empty;
        }
        catch (ArgumentException)
        {
            extension = string.Empty;
        }

        return IsSafeMultipartName(extension) ? "upload" + extension : "upload";
    }

    private static bool IsSafeMultipartName(string value)
        => !string.IsNullOrWhiteSpace(value)
           && !value.Contains('"')
           && !value.Contains('\r')
           && !value.Contains('\n');
}
