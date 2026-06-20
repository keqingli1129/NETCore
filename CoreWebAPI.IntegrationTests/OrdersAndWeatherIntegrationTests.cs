using System.Net;
using System.Net.Http.Json;
using CoreMVC.Contracts.Orders;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreWebAPI.IntegrationTests;

public class WeatherForecastIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WeatherForecastIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsOkAndFiveItems()
    {
        var response = await _client.GetAsync("/WeatherForecast");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        forecasts.Should().NotBeNull().And.HaveCount(5);
    }

    [Fact]
    public async Task GetWeatherForecast_ItemsHaveExpectedProperties()
    {
        var forecasts = await _client.GetFromJsonAsync<WeatherForecast[]>("/WeatherForecast");

        forecasts.Should().AllSatisfy(f =>
        {
            f.Date.Should().BeAfter(DateOnly.FromDateTime(DateTime.Now));
            f.Summary.Should().NotBeNullOrEmpty();
        });
    }
}

public class OrdersIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrdersIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Orders.RemoveRange(db.Orders);
        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetOrders_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/Orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<OrderDto[]>();
        orders.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task PostAndGetOrder_RoundTrip()
    {
        var newOrder = new CreateOrderDto
        {
            CustomerId = "ALFKI",
            ShipName = "Test Ship",
            ShipCity = "TestCity",
            ShipCountry = "TestCountry"
        };

        var postResponse = await _client.PostAsJsonAsync("/api/Orders", newOrder);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await postResponse.Content.ReadFromJsonAsync<OrderDto>();
        created.Should().NotBeNull();
        created!.ShipName.Should().Be("Test Ship");

        var getResponse = await _client.GetAsync($"/api/Orders/{created.OrderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrder_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/Orders/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOrder_ExistingOrder_ReturnsNoContent()
    {
        // Arrange – seed an order directly via DbContext
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = new Order { ShipName = "ToDelete", ShipCity = "City" };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var orderId = order.OrderId;

        // Act
        var response = await _client.DeleteAsync($"/api/Orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
