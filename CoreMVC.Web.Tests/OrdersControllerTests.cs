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
using CoreMVC.Contracts.Orders;
using CoreMVC.Domain.Entities;
using Xunit;

namespace CoreMVC.Web.Tests;

public class OrdersControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewWithOrdersFromApi()
    {
        // Arrange
        var order1 = new OrderDto { OrderId = 1, OrderDate = new DateTime(2023, 1, 1), ShipCity = "A" };
        var order2 = new OrderDto { OrderId = 2, OrderDate = new DateTime(2024, 1, 1), ShipCity = "B" };
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
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<OrderDto>>().Subject;
        model.Should().HaveCount(2);
    }

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithOrder()
    {
        // Arrange
        var order = new OrderDto
        {
            OrderId = 42,
            OrderDate = new DateTime(2024, 4, 2),
            ShipCity = "City",
            CustomerName = "Acme",
            EmployeeName = "John Doe",
            Shipper = "FastShip"
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
        var model = viewResult.Model.Should().BeAssignableTo<OrderDto>().Subject;
        model.OrderId.Should().Be(42);
        model.CustomerName.Should().Be("Acme");
        model.EmployeeName.Should().Be("John Doe");
        model.Shipper.Should().Be("FastShip");
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
