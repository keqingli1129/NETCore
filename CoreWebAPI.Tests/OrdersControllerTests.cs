using CoreMVC.Contracts.Orders;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using CoreWebAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreWebAPI.Tests;

public class OrdersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new OrdersController(_context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private Order CreateOrder(int orderId, string customerId = "ALFKI") =>
        new() { OrderId = orderId, CustomerId = customerId, ShipName = $"Ship-{orderId}" };

    private static CreateOrderDto CreateOrderDto(string customerId = "ALFKI", string? shipName = null) =>
        new() { CustomerId = customerId, ShipName = shipName ?? "Ship-new" };

    private async Task SeedOrdersAsync(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            _context.Orders.Add(CreateOrder(i));
        }
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetOrders_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _controller.GetOrders();

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrders_ReturnsPagedResults()
    {
        await SeedOrdersAsync(15);

        var result = await _controller.GetOrders(pageNumber: 1, pageSize: 10);

        result.Value.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetOrders_SecondPage_ReturnsRemainingItems()
    {
        await SeedOrdersAsync(15);

        var result = await _controller.GetOrders(pageNumber: 2, pageSize: 10);

        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetOrders_SetsPaginationHeaders()
    {
        await SeedOrdersAsync(5);

        await _controller.GetOrders(pageNumber: 1, pageSize: 10);

        var headers = _controller.Response.Headers;
        headers["X-Total-Count"].ToString().Should().Be("5");
        headers["X-Page-Number"].ToString().Should().Be("1");
        headers["X-Page-Size"].ToString().Should().Be("10");
    }

    [Fact]
    public async Task GetOrders_ClampsPageSizeToMax100()
    {
        await SeedOrdersAsync(5);

        await _controller.GetOrders(pageNumber: 1, pageSize: 200);

        _controller.Response.Headers["X-Page-Size"].ToString().Should().Be("100");
    }

    [Fact]
    public async Task GetOrder_WhenExists_ReturnsOrder()
    {
        _context.Orders.Add(CreateOrder(1));
        await _context.SaveChangesAsync();

        var result = await _controller.GetOrder(1);

        result.Value.Should().NotBeNull();
        result.Value!.OrderId.Should().Be(1);
    }

    [Fact]
    public async Task GetOrder_WhenNotFound_ReturnsNotFound()
    {
        var result = await _controller.GetOrder(999);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PostOrder_CreatesAndReturnsCreatedAtAction()
    {
        var dto = CreateOrderDto();

        var result = await _controller.PostOrder(dto);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _context.Orders.Should().HaveCount(1);
    }

    [Fact]
    public async Task PutOrder_WhenValid_ReturnsNoContent()
    {
        var order = CreateOrder(1);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _controller.PutOrder(1, CreateOrderDto(shipName: "Updated"));

        result.Should().BeOfType<NoContentResult>();
        var saved = await _context.Orders.FindAsync(1);
        saved!.ShipName.Should().Be("Updated");
    }

    [Fact]
    public async Task PutOrder_WhenNotFound_ReturnsNotFound()
    {
        var result = await _controller.PutOrder(999, CreateOrderDto());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteOrder_WhenExists_ReturnsNoContent()
    {
        _context.Orders.Add(CreateOrder(1));
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteOrder(1);

        result.Should().BeOfType<NoContentResult>();
        _context.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteOrder_WhenNotFound_ReturnsNotFound()
    {
        var result = await _controller.DeleteOrder(999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
