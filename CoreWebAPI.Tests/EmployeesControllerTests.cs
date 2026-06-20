using System;
using System.Threading.Tasks;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using CoreMVC.Contracts.Common;
using CoreMVC.Contracts.Employees;
using CoreWebAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreWebAPI.Tests;

public class EmployeesControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new EmployeesController(_context)
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

    private static Employee CreateEmployee(int id) => new()
    {
        EmployeeId = id,
        LastName = $"Last{id}",
        FirstName = $"First{id}",
        Title = "Developer"
    };

    private async Task SeedEmployeesAsync(int count)
    {
        for (var i = 1; i <= count; i++)
            _context.Employees.Add(CreateEmployee(i));
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetEmployees_WhenEmpty_ReturnsEmptyPage()
    {
        var result = await _controller.GetEmployees();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<EmployeeDto>>().Subject;
        paged.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetEmployees_ReturnsPagedResults()
    {
        await SeedEmployeesAsync(15);

        var result = await _controller.GetEmployees(page: 1, pageSize: 10);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<EmployeeDto>>().Subject;
        paged.Items.Should().HaveCount(10);
        paged.TotalCount.Should().Be(15);
    }

    [Fact]
    public async Task GetEmployees_SecondPage_ReturnsRemaining()
    {
        await SeedEmployeesAsync(15);

        var result = await _controller.GetEmployees(page: 2, pageSize: 10);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var paged = ok.Value.Should().BeOfType<PagedResult<EmployeeDto>>().Subject;
        paged.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetEmployee_WhenExists_ReturnsOk()
    {
        _context.Employees.Add(CreateEmployee(1));
        await _context.SaveChangesAsync();

        var result = await _controller.GetEmployee(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var employee = ok.Value.Should().BeOfType<Employee>().Subject;
        employee.EmployeeId.Should().Be(1);
    }

    [Fact]
    public async Task GetEmployee_WhenNotFound_ReturnsNotFound()
    {
        var result = await _controller.GetEmployee(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedAtAction()
    {
        var dto = new CreateEmployeeDto { LastName = "Doe", FirstName = "John", Title = "Dev" };

        var result = await _controller.CreateEmployee(dto);

        result.Should().BeOfType<CreatedAtActionResult>();
        _context.Employees.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateEmployee_WhenExists_ReturnsNoContent()
    {
        _context.Employees.Add(CreateEmployee(1));
        await _context.SaveChangesAsync();

        var dto = new CreateEmployeeDto { LastName = "Updated", FirstName = "Name" };

        var result = await _controller.UpdateEmployee(1, dto);

        result.Should().BeOfType<NoContentResult>();
        var saved = await _context.Employees.FindAsync(1);
        saved!.LastName.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateEmployee_WhenNotFound_ReturnsNotFound()
    {
        var dto = new CreateEmployeeDto { LastName = "X", FirstName = "Y" };

        var result = await _controller.UpdateEmployee(999, dto);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteEmployee_WhenExists_ReturnsNoContent()
    {
        _context.Employees.Add(CreateEmployee(1));
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteEmployee(1);

        result.Should().BeOfType<NoContentResult>();
        _context.Employees.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEmployee_WhenNotFound_ReturnsNotFound()
    {
        var result = await _controller.DeleteEmployee(999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
