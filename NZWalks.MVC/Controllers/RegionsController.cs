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

    private static FileParameter? ToFileParameter(IFormFile? file)
        => file is null || file.Length == 0
            ? null
            : new FileParameter(file.OpenReadStream(), file.FileName, file.ContentType);
}
