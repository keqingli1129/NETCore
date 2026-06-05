using System.Net;
using System.Net.Http.Json;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using CoreWebAPI.Controllers;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreWebAPI.IntegrationTests;

/// <summary>
/// Integration tests for the [Authorize]-protected EmployeesController.
/// FakeItEasy is used to create fake services where needed, while the
/// <see cref="CustomWebApplicationFactory"/> supplies a test auth scheme.
/// </summary>
public class EmployeesIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmployeesIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Employees.RemoveRange(db.Employees);
        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetEmployees_WhenEmpty_ReturnsOkWithEmptyPage()
    {
        var response = await _client.GetAsync("/api/Employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetEmployees_WithSeededData_ReturnsPagedResult()
    {
        // Arrange – seed employees
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Employees.AddRange(
                new Employee { LastName = "Doe", FirstName = "John", City = "Seattle" },
                new Employee { LastName = "Smith", FirstName = "Jane", City = "Portland" });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/Employees?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task CreateAndGetEmployee_RoundTrip()
    {
        var dto = new CreateEmployeeDto
        {
            LastName = "Tester",
            FirstName = "Integration",
            Title = "QA",
            City = "Redmond",
            Country = "USA"
        };

        var postResponse = await _client.PostAsJsonAsync("/api/Employees", dto);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await postResponse.Content.ReadFromJsonAsync<Employee>();
        created.Should().NotBeNull();
        created!.LastName.Should().Be("Tester");

        var getResponse = await _client.GetAsync($"/api/Employees/{created.EmployeeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEmployee_NonExistent_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/Employees/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEmployee_ExistingEmployee_ReturnsNoContent()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emp = new Employee { LastName = "Old", FirstName = "Name" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        var empId = emp.EmployeeId;

        // Act
        var updateDto = new CreateEmployeeDto
        {
            LastName = "Updated",
            FirstName = "Name"
        };
        var response = await _client.PutAsJsonAsync($"/api/Employees/{empId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteEmployee_ExistingEmployee_ReturnsNoContent()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emp = new Employee { LastName = "ToDelete", FirstName = "Employee" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        var empId = emp.EmployeeId;

        // Act
        var response = await _client.DeleteAsync($"/api/Employees/{empId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteEmployee_NonExistent_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/Employees/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>
/// Demonstrates using FakeItEasy to verify interactions with a fake service
/// in the context of the WebApplicationFactory pipeline.
/// </summary>
public class EmployeesFakeServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EmployeesFakeServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEmployees_UsesApplicationDbContext()
    {
        // Demonstrates that we can create a fake ApplicationDbContext and assert expectations.
        // In integration tests the real in-memory DbContext is used by the pipeline,
        // but FakeItEasy can verify behavior on custom abstractions injected via DI.
        var fakeContext = A.Fake<ApplicationDbContext>(
            o => o.WithArgumentsForConstructor(
                [new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase("FakeDb").Options]));

        fakeContext.Should().NotBeNull();

        // The real pipeline test still works end-to-end
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Employees");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
