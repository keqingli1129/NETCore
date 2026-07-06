
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlainNetCoreMVC.ApiClients;
using PlainNetCoreMVC.Models;

public class CategoriesController : Controller
{
    private readonly MVCNetContext _context;
    private readonly IPlainNetCoreWebApiClient _apiClient;

    public CategoriesController(MVCNetContext context, IPlainNetCoreWebApiClient apiClient)
    {
        _context = context;
        _apiClient = apiClient;
    }

    private const int PageSize = 5;

    // GET: Categories
    public async Task<IActionResult> Index(int? pageNumber)
    {
        var categories = _context.Categories.OrderBy(c => c.CategoryName);
        return View(await PaginatedList<Category>.CreateAsync(categories, pageNumber ?? 1, PageSize));
    }

    // GET: Categories/FromApi — fetches categories from PlainNetCoreWebAPI via the NSwag-generated client
    public async Task<IActionResult> FromApi(int? pageNumber)
    {
        try
        {
            var result = await _apiClient.CategoriesGETAsync(pageNumber ?? 1, PageSize);
            return View(new PaginatedList<CategoryDto>(
                result.Items.ToList(), result.TotalCount, result.Page, result.PageSize));
        }
        catch (Exception ex) when (ex is HttpRequestException or ApiException)
        {
            ViewBag.ErrorMessage = "Could not reach PlainNetCoreWebAPI. Make sure the API is running.";
            return View(new PaginatedList<CategoryDto>([], 0, 1, PageSize));
        }
    }

    // GET: Categories/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(m => m.CategoryId == id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // GET: Categories/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,Description")] Category category, IFormFile? picture)
    {
        if (ModelState.IsValid)
        {
            if (picture is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await picture.CopyToAsync(ms);
                category.Picture = ms.ToArray();
            }

            _context.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: Categories/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // POST: Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName,Description")] Category category, IFormFile? picture)
    {
        if (id != category.CategoryId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (picture is { Length: > 0 })
                {
                    using var ms = new MemoryStream();
                    await picture.CopyToAsync(ms);
                    category.Picture = ms.ToArray();
                }
                else
                {
                    var existing = await _context.Categories
                        .AsNoTracking()
                        .Where(c => c.CategoryId == id)
                        .Select(c => c.Picture)
                        .FirstOrDefaultAsync();
                    category.Picture = existing;
                }

                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.CategoryId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: Categories/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(m => m.CategoryId == id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // POST: Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CategoryExists(int id)
    {
        return _context.Categories.Any(e => e.CategoryId == id);
    }
}
