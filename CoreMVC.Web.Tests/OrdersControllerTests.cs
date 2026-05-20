using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreMVC.Web.Controllers;
using CoreMVC.Infrastructure.Data;
using CoreMVC.Domain.Entities;
using Xunit;

namespace CoreMVC.Web.Tests;

public class OrdersControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewWithOrdersOrderedByDateDesc()
    {
        // Arrange
        var order1 = new Order { OrderId = 1, OrderDate = new DateTime(2023, 1, 1), ShipCity = "A" };
        var order2 = new Order { OrderId = 2, OrderDate = new DateTime(2024, 1, 1), ShipCity = "B" };
        var orders = new[] { order1, order2 };

        // Use an in-memory DbContext for more reliable behavior
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Orders.AddRange(orders);
        context.SaveChanges();

        var controller = new OrdersController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Order>>().Subject;
        model.Select(o => o.OrderId).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithOrder()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 42,
            OrderDate = new DateTime(2024, 4, 2),
            ShipCity = "City",
            Customer = new Customer { CustomerId = "CUST42", CompanyName = "Acme" },
            Employee = new Employee { EmployeeId = 1, FirstName = "John", LastName = "Doe" },
            ShipViaNavigation = new Shipper { ShipperId = 1, CompanyName = "FastShip" }
        };

        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Orders.Add(order);
        context.SaveChanges();

        var controller = new OrdersController(context);

        // Act
        var result = await controller.Details(order.OrderId);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<Order>().Subject;
        model.OrderId.Should().Be(order.OrderId);
        model.Customer.CompanyName.Should().Be("Acme");
        model.Employee.LastName.Should().Be("Doe");
        model.ShipViaNavigation.CompanyName.Should().Be("FastShip");
    }
}
