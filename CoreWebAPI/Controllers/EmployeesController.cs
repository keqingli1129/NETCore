using CoreMVC.Contracts.Common;
using CoreMVC.Contracts.Employees;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using CoreWebAPI.Mapping;
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
        var query = context.Employees
            .OrderBy(e => e.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var employees = await EmployeeMapper.ProjectToDto(query).ToListAsync(cancellationToken);

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

        return Ok(EmployeeMapper.ToDto(employee));
    }

    /// <summary>
    /// Creates a new employee.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
    {
        var employee = EmployeeMapper.ToEntity(dto);

        context.Employees.Add(employee);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, EmployeeMapper.ToDto(employee));
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

        EmployeeMapper.Update(dto, employee);

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
