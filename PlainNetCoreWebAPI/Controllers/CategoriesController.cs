using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlainNetCoreWebAPI.Dtos;
using PlainNetCoreWebAPI.Models;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly MVCNetContext _context;
    private readonly IMapper _mapper;

    public CategoriesController(MVCNetContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET: api/Categories?page=1&pageSize=10&search=bev
    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryDto>>> GetCategory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.CategoryName.Contains(term) ||
                (c.Description != null && c.Description.Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.CategoryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<CategoryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // GET: api/Categories/5
    [HttpGet("{categoryid}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int categoryid)
    {
        var category = await _context.Categories.FindAsync(categoryid);

        if (category == null)
        {
            return NotFound();
        }

        return _mapper.Map<CategoryDto>(category);
    }

    // PUT: api/Categories/5
    [HttpPut("{categoryid}")]
    public async Task<IActionResult> PutCategory(int categoryid, CategoryDto categoryDto)
    {
        if (categoryid != categoryDto.CategoryId)
        {
            return BadRequest();
        }

        var category = await _context.Categories.FindAsync(categoryid);
        if (category == null)
        {
            return NotFound();
        }

        _mapper.Map(categoryDto, category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/Categories
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> PostCategory(CategoryDto categoryDto)
    {
        var category = _mapper.Map<Category>(categoryDto);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var created = _mapper.Map<CategoryDto>(category);
        return CreatedAtAction("GetCategory", new { categoryid = created.CategoryId }, created);
    }

    // DELETE: api/Categories/5
    [HttpDelete("{categoryid}")]
    public async Task<IActionResult> DeleteCategory(int? categoryid)
    {
        var category = await _context.Categories.FindAsync(categoryid);
        if (category == null)
        {
            return NotFound();
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
