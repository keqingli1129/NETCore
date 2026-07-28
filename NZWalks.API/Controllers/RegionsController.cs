using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models;

[Route("api/[controller]")]
[ApiController]
public class RegionsController : ControllerBase
{
    private readonly MVCNetNZWalksContext _context;
    public RegionsController(MVCNetNZWalksContext context)
    {
        _context = context;
    }

    // GET: api/Region
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Region>>> GetRegion()
    {
        return await _context.Regions.ToListAsync();
    }

    // GET: api/Region/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Region>> GetRegion(System.Guid id)
    {
        var region = await _context.Regions.FindAsync(id);

        if (region == null)
        {
            return NotFound();
        }

        return region;
    }

    // PUT: api/Region/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRegion(System.Guid? id, Region region)
    {
        if (id != region.Id)
        {
            return BadRequest();
        }

        _context.Entry(region).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RegionExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Region
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Region>> PostRegion(Region region)
    {
        _context.Regions.Add(region);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetRegion", new { id = region.Id }, region);
    }

    // DELETE: api/Region/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRegion(System.Guid? id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null)
        {
            return NotFound();
        }

        _context.Regions.Remove(region);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool RegionExists(System.Guid? id)
    {
        return _context.Regions.Any(e => e.Id == id);
    }
}
