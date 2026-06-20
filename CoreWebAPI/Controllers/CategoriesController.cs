using CoreMVC.Contracts.Categories;
using CoreMVC.Contracts.Common;
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
        var query = context.Categories
            .OrderBy(c => c.CategoryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var categories = await CategoryMapper.ProjectToDto(query).ToListAsync(cancellationToken);

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

        return Ok(CategoryMapper.ToDto(category));
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = CategoryMapper.ToEntity(dto);

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, CategoryMapper.ToDto(category));
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

        CategoryMapper.Update(dto, category);

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
