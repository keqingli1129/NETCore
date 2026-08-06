using Microsoft.AspNetCore.Mvc;

namespace NZWalks.MVC.Controllers;

public class RegionsController(APIClient apiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var regions = await apiClient.RegionsAllAsync(ct);
        return View(regions);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var region = await apiClient.RegionsGETAsync(id, ct);
        return View(region);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddRegionRequestDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        await apiClient.RegionsPOSTAsync(dto, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var region = await apiClient.RegionsGETAsync(id, ct);
        return View(new UpdateRegionRequestDto
        {
            Code = region.Code,
            Name = region.Name,
            RegionImageUrl = region.RegionImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateRegionRequestDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        await apiClient.RegionsPUTAsync(id, dto, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await apiClient.RegionsDELETEAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
