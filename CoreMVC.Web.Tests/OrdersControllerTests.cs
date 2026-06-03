using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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
    public async Task Index_ReturnsViewWithOrdersFromApi()
    {
        // Arrange
        var order1 = new Order { OrderId = 1, OrderDate = new DateTime(2023, 1, 1), ShipCity = "A" };
        var order2 = new Order { OrderId = 2, OrderDate = new DateTime(2024, 1, 1), ShipCity = "B" };
        var orders = new[] { order2, order1 };

        var json = JsonSerializer.Serialize(orders);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        responseMessage.Headers.Add("X-Total-Count", "2");

        var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
        {
            BaseAddress = new Uri("https://localhost:7127")
        };

        var httpClientFactory = A.Fake<IHttpClientFactory>();
        A.CallTo(() => httpClientFactory.CreateClient("OrdersApi")).Returns(httpClient);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);

        var controller = new OrdersController(context, httpClientFactory);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Order>>().Subject;
        model.Should().HaveCount(2);
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

        var json = JsonSerializer.Serialize(order);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
        {
            BaseAddress = new Uri("https://localhost:7127")
        };

        var httpClientFactory = A.Fake<IHttpClientFactory>();
        A.CallTo(() => httpClientFactory.CreateClient("OrdersApi")).Returns(httpClient);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);

        var controller = new OrdersController(context, httpClientFactory);

        // Act
        var result = await controller.Details(order.OrderId);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<Order>().Subject;
        model.OrderId.Should().Be(42);
        model.Customer.CompanyName.Should().Be("Acme");
        model.Employee.LastName.Should().Be("Doe");
        model.ShipViaNavigation.CompanyName.Should().Be("FastShip");
    }
}

/// <summary>
/// A simple HttpMessageHandler that returns a preconfigured response.
/// </summary>
internal sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        return Task.FromResult(response);
    }
}
