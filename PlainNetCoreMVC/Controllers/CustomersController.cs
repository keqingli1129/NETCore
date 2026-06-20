
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlainNetCoreMVC.Models;

public class CustomersController : Controller
{
    private readonly MVCNetContext _context;

    public CustomersController(MVCNetContext context)
    {
        _context = context;
    }

    private const int PageSize = 10;

    // GET: CUSTOMERS
    public async Task<IActionResult> Index(int? pageNumber)
    {
        var customers = _context.Customers.OrderBy(c => c.CompanyName);
        return View(await PaginatedList<Customer>.CreateAsync(customers, pageNumber ?? 1, PageSize));
    }

    // GET: CUSTOMERS/Details/5
    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .Include(c => c.CustomerTypes)
            .FirstOrDefaultAsync(m => m.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        await PopulateDropdownsAsync(customer.Region);
        return View(customer);
    }

    // GET: CUSTOMERS/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    // POST: CUSTOMERS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CustomerId,CompanyName,ContactName,ContactTitle,Address,City,Region,PostalCode,Country,Phone,Fax")] Customer customer,
        string selectedCustomerDemographicId)
    {
        if (ModelState.IsValid)
        {
            _context.Add(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        await PopulateDropdownsAsync(customer.Region);
        return View(customer);
    }

    // GET: CUSTOMERS/Edit/5
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .Include(c => c.CustomerTypes)
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }
        await PopulateDropdownsAsync(customer.Region);
        return View(customer);
    }

    // POST: CUSTOMERS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string? id,
        [Bind("CustomerId,CompanyName,ContactName,ContactTitle,Address,City,Region,PostalCode,Country,Phone,Fax")] Customer customer,
        string selectedCustomerDemographicId)
    {
        if (id != customer.CustomerId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingCustomer = await _context.Customers
                    .Include(c => c.CustomerTypes)
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                if (existingCustomer == null)
                {
                    return NotFound();
                }

                _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
                existingCustomer.CustomerTypes.Clear();
                await AttachSelectedCustomerDemographicAsync(existingCustomer, selectedCustomerDemographicId);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.CustomerId))
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
        await PopulateDropdownsAsync(customer.Region);
        return View(customer);
    }

    // GET: CUSTOMERS/Delete/5
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustomerId == id);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // POST: CUSTOMERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CustomerExists(string? id)
    {
        return _context.Customers.Any(e => e.CustomerId == id);
    }

    private async Task PopulateDropdownsAsync(string selectedRegion = null)
    {
        var regions = await _context.Regions
            .OrderBy(r => r.RegionDescription)
            .Select(r => r.RegionDescription.Trim())
            .ToListAsync();
        ViewBag.RegionList = new SelectList(regions, selectedRegion);
       
    }

    private async Task AttachSelectedCustomerDemographicAsync(Customer customer, string selectedCustomerDemographicId)
    {
        if (!string.IsNullOrWhiteSpace(selectedCustomerDemographicId))
        {
            var demographic = await _context.CustomerDemographics.FindAsync(selectedCustomerDemographicId);
            if (demographic != null)
            {
                customer.CustomerTypes.Add(demographic);
            }
        }
    }
}
