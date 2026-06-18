using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreWebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Gets a paged list of categories.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var totalCount = await context.Categories.CountAsync(cancellationToken);
        var categories = await context.Categories
            .OrderBy(c => c.CategoryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<CategoryDto>
        {
            Items = categories,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
            return NotFound();

        return Ok(new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description
        });
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            CategoryName = dto.CategoryName,
            Description = dto.Description
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description
        });
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
            return NotFound();

        category.CategoryName = dto.CategoryName;
        category.Description = dto.Description;

        await context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Deletes a category by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken);
        if (category is null)
            return NotFound();

        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public record CategoryDto
{
    public int CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? Description { get; init; }
}

public record CreateCategoryDto
{
    public required string CategoryName { get; init; }
    public string? Description { get; init; }
}
