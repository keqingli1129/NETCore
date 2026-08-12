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
