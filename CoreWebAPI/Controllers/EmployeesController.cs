using CoreMVC.Contracts.Common;
using CoreMVC.Contracts.Employees;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreWebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController(ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Gets a paged list of employees.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEmployees([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var totalCount = await context.Employees.CountAsync(cancellationToken);
        var employees = await context.Employees
            .OrderBy(e => e.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                LastName = e.LastName,
                FirstName = e.FirstName,
                Title = e.Title,
                City = e.City,
                Country = e.Country,
                HireDate = e.HireDate,
                ReportsTo = e.ReportsTo
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<EmployeeDto>
        {
            Items = employees,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Gets an employee by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(int id, CancellationToken cancellationToken = default)
    {
        var employee = await context.Employees.FindAsync([id], cancellationToken);
        if (employee is null)
            return NotFound();

        return Ok(employee);
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        var employee = new Employee
        {
            LastName = dto.LastName,
            FirstName = dto.FirstName,
            Title = dto.Title,
            BirthDate = dto.BirthDate,
            HireDate = dto.HireDate,
            Address = dto.Address,
            City = dto.City,
            Region = dto.Region,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            HomePhone = dto.HomePhone,
            ReportsTo = dto.ReportsTo
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, employee);
    }

    /// <summary>
    /// Updates an existing employee.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, CreateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        var employee = await context.Employees.FindAsync([id], cancellationToken);
        if (employee is null)
            return NotFound();

        employee.LastName = dto.LastName;
        employee.FirstName = dto.FirstName;
        employee.Title = dto.Title;
        employee.BirthDate = dto.BirthDate;
        employee.HireDate = dto.HireDate;
        employee.Address = dto.Address;
        employee.City = dto.City;
        employee.Region = dto.Region;
        employee.PostalCode = dto.PostalCode;
        employee.Country = dto.Country;
        employee.HomePhone = dto.HomePhone;
        employee.ReportsTo = dto.ReportsTo;

        await context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Deletes an employee by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id, CancellationToken cancellationToken = default)
    {
        var employee = await context.Employees.FindAsync([id], cancellationToken);
        if (employee is null)
            return NotFound();

        context.Employees.Remove(employee);
        await context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
