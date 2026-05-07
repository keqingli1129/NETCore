using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoreMVC.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists all orders with customer and employee info.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.ShipViaNavigation)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    /// <summary>
    /// Shows order details including line items.
    /// </summary>
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.ShipViaNavigation)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    /// <summary>
    /// Shows the create order form.
    /// </summary>
    public IActionResult Create()
    {
        PopulateDropdowns();
        return View();
    }

    /// <summary>
    /// Handles order creation.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerId,EmployeeId,OrderDate,RequiredDate,ShipVia,Freight,ShipName,ShipAddress,ShipCity,ShipRegion,ShipPostalCode,ShipCountry")] Order order)
    {
        ModelState.Remove(nameof(Order.Customer));
        ModelState.Remove(nameof(Order.Employee));
        ModelState.Remove(nameof(Order.ShipViaNavigation));
        ModelState.Remove(nameof(Order.OrderDetails));

        if (ModelState.IsValid)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        PopulateDropdowns(order);
        return View(order);
    }

    /// <summary>
    /// Shows the edit order form.
    /// </summary>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        PopulateDropdowns(order);
        return View(order);
    }

    /// <summary>
    /// Handles order update.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("OrderId,CustomerId,EmployeeId,OrderDate,RequiredDate,ShippedDate,ShipVia,Freight,ShipName,ShipAddress,ShipCity,ShipRegion,ShipPostalCode,ShipCountry")] Order order)
    {
        if (id != order.OrderId)
        {
            return NotFound();
        }

        ModelState.Remove(nameof(Order.Customer));
        ModelState.Remove(nameof(Order.Employee));
        ModelState.Remove(nameof(Order.ShipViaNavigation));
        ModelState.Remove(nameof(Order.OrderDetails));

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await OrderExistsAsync(order.OrderId))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        PopulateDropdowns(order);
        return View(order);
    }

    /// <summary>
    /// Shows delete confirmation page.
    /// </summary>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.ShipViaNavigation)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    /// <summary>
    /// Handles order deletion.
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is not null)
        {
            _context.OrderDetails.RemoveRange(order.OrderDetails);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> OrderExistsAsync(int id)
    {
        return await _context.Orders.AnyAsync(o => o.OrderId == id);
    }

    private void PopulateDropdowns(Order order = null)
    {
        ViewData["CustomerId"] = new SelectList(
            _context.Customers.OrderBy(c => c.CompanyName),
            nameof(Customer.CustomerId),
            nameof(Customer.CompanyName),
            order?.CustomerId);

        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.OrderBy(e => e.LastName),
            nameof(Employee.EmployeeId),
            nameof(Employee.LastName),
            order?.EmployeeId);

        ViewData["ShipVia"] = new SelectList(
            _context.Shippers.OrderBy(s => s.CompanyName),
            nameof(Shipper.ShipperId),
            nameof(Shipper.CompanyName),
            order?.ShipVia);
    }
}
