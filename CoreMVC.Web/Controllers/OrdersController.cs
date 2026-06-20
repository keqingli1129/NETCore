using System.Net;
using System.Text;
using System.Text.Json;
using CoreMVC.Contracts.Orders;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoreMVC.Web.Controllers;

[Authorize(Roles = "Admin,User")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public OrdersController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Lists orders by calling the Orders API with paging support.
    /// </summary>
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var client = _httpClientFactory.CreateClient("OrdersApi");
        using var response = await client.GetAsync($"api/Orders?pageNumber={pageNumber}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var orders = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, s_jsonOptions) ?? [];

        var totalCount = 0;
        if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
        {
            int.TryParse(totalCountValues.FirstOrDefault(), out totalCount);
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        ViewData["PageNumber"] = pageNumber;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;

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

        var client = _httpClientFactory.CreateClient("OrdersApi");
        using var response = await client.GetAsync($"api/Orders/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var order = await JsonSerializer.DeserializeAsync<OrderDto>(stream, s_jsonOptions);

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
    /// Handles order creation via the API.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        if (ModelState.IsValid)
        {
            var client = _httpClientFactory.CreateClient("OrdersApi");
            var json = JsonSerializer.Serialize(dto, s_jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("api/Orders", content);
            response.EnsureSuccessStatusCode();

            return RedirectToAction(nameof(Index));
        }

        PopulateDropdowns(dto.CustomerId, dto.EmployeeId, dto.ShipVia);
        return View(dto);
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

        var client = _httpClientFactory.CreateClient("OrdersApi");
        using var response = await client.GetAsync($"api/Orders/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var order = await JsonSerializer.DeserializeAsync<OrderDto>(stream, s_jsonOptions);

        if (order is null)
        {
            return NotFound();
        }

        var dto = ToCreateDto(order);

        ViewData["OrderId"] = order.OrderId;
        PopulateDropdowns(dto.CustomerId, dto.EmployeeId, dto.ShipVia);
        return View(dto);
    }

    /// <summary>
    /// Handles order update via the API.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateOrderDto dto)
    {
        if (ModelState.IsValid)
        {
            var client = _httpClientFactory.CreateClient("OrdersApi");
            var json = JsonSerializer.Serialize(dto, s_jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PutAsync($"api/Orders/{id}", content);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            response.EnsureSuccessStatusCode();
            return RedirectToAction(nameof(Index));
        }

        ViewData["OrderId"] = id;
        PopulateDropdowns(dto.CustomerId, dto.EmployeeId, dto.ShipVia);
        return View(dto);
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

        var client = _httpClientFactory.CreateClient("OrdersApi");
        using var response = await client.GetAsync($"api/Orders/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var order = await JsonSerializer.DeserializeAsync<OrderDto>(stream, s_jsonOptions);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    /// <summary>
    /// Handles order deletion via the API.
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = _httpClientFactory.CreateClient("OrdersApi");
        using var response = await client.DeleteAsync($"api/Orders/{id}");

        // Ignore NotFound — order may have already been deleted.
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }

        return RedirectToAction(nameof(Index));
    }

    private static CreateOrderDto ToCreateDto(OrderDto order) => new()
    {
        CustomerId = order.CustomerId ?? string.Empty,
        EmployeeId = order.EmployeeId,
        OrderDate = order.OrderDate,
        RequiredDate = order.RequiredDate,
        ShippedDate = order.ShippedDate,
        ShipVia = order.ShipVia,
        Freight = order.Freight,
        ShipName = order.ShipName,
        ShipAddress = order.ShipAddress,
        ShipCity = order.ShipCity,
        ShipRegion = order.ShipRegion,
        ShipPostalCode = order.ShipPostalCode,
        ShipCountry = order.ShipCountry
    };

    private void PopulateDropdowns(string? customerId = null, int? employeeId = null, int? shipVia = null)
    {
        ViewData["CustomerId"] = new SelectList(
            _context.Customers.OrderBy(c => c.CompanyName),
            nameof(Customer.CustomerId),
            nameof(Customer.CompanyName),
            customerId);

        ViewData["EmployeeId"] = new SelectList(
            _context.Employees.OrderBy(e => e.LastName),
            nameof(Employee.EmployeeId),
            nameof(Employee.LastName),
            employeeId);

        ViewData["ShipVia"] = new SelectList(
            _context.Shippers.OrderBy(s => s.CompanyName),
            nameof(Shipper.ShipperId),
            nameof(Shipper.CompanyName),
            shipVia);
    }
}
