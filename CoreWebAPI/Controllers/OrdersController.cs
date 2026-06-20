using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CoreMVC.Contracts.Orders;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Single source of truth for Order -> OrderDto mapping, reused by both
        // the IQueryable projections (translated to SQL) and the post-write mapping.
        private static readonly Expression<Func<Order, OrderDto>> ToDto = o => new OrderDto
        {
            OrderId = o.OrderId,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? o.Customer.CompanyName : null,
            EmployeeId = o.EmployeeId,
            EmployeeName = o.Employee != null ? o.Employee.FirstName + " " + o.Employee.LastName : null,
            OrderDate = o.OrderDate,
            RequiredDate = o.RequiredDate,
            ShippedDate = o.ShippedDate,
            ShipVia = o.ShipVia,
            Shipper = o.ShipViaNavigation != null ? o.ShipViaNavigation.CompanyName : null,
            Freight = o.Freight,
            ShipName = o.ShipName,
            ShipAddress = o.ShipAddress,
            ShipCity = o.ShipCity,
            ShipRegion = o.ShipRegion,
            ShipPostalCode = o.ShipPostalCode,
            ShipCountry = o.ShipCountry,
            OrderDetails = o.OrderDetails.Select(od => new OrderDetailDto
            {
                ProductId = od.ProductId,
                ProductName = od.Product != null ? od.Product.ProductName : null,
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                Discount = od.Discount
            }).ToList()
        };

        private static readonly Func<Order, OrderDto> ToDtoCompiled = ToDto.Compile();

        // GET: api/Orders?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var totalCount = await _context.Orders.CountAsync();

            var orders = await _context.Orders
                .AsNoTracking()
                .OrderBy(o => o.OrderId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDto)
                .ToListAsync();

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page-Number"] = pageNumber.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return orders;
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderId == id)
                .Select(ToDto)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, CreateOrderDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            MapInto(order, dto);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<OrderDto>> PostOrder(CreateOrderDto dto)
        {
            var order = new Order();
            MapInto(order, dto);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, ToDtoCompiled(order));
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static void MapInto(Order order, CreateOrderDto dto)
        {
            order.CustomerId = dto.CustomerId;
            order.EmployeeId = dto.EmployeeId;
            order.OrderDate = dto.OrderDate;
            order.RequiredDate = dto.RequiredDate;
            order.ShippedDate = dto.ShippedDate;
            order.ShipVia = dto.ShipVia;
            order.Freight = dto.Freight;
            order.ShipName = dto.ShipName;
            order.ShipAddress = dto.ShipAddress;
            order.ShipCity = dto.ShipCity;
            order.ShipRegion = dto.ShipRegion;
            order.ShipPostalCode = dto.ShipPostalCode;
            order.ShipCountry = dto.ShipCountry;
        }
    }
}
