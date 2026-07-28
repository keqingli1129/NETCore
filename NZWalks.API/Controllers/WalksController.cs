using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models;

[Route("api/[controller]")]
[ApiController]
public class WalksController : ControllerBase
{
    private readonly MVCNetNZWalksContext _context;
    public WalksController(MVCNetNZWalksContext context)
    {
        _context = context;
    }

    // GET: api/Walk
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Walk>>> GetWalk()
    {
        return await _context.Walks.ToListAsync();
    }

    // GET: api/Walk/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Walk>> GetWalk(System.Guid id)
    {
        var walk = await _context.Walks.FindAsync(id);

        if (walk == null)
        {
            return NotFound();
        }

        return walk;
    }

    // PUT: api/Walk/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutWalk(System.Guid? id, Walk walk)
    {
        if (id != walk.Id)
        {
            return BadRequest();
        }

        _context.Entry(walk).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WalkExists(id))
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

    // POST: api/Walk
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Walk>> PostWalk(Walk walk)
    {
        _context.Walks.Add(walk);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetWalk", new { id = walk.Id }, walk);
    }

    // DELETE: api/Walk/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWalk(System.Guid? id)
    {
        var walk = await _context.Walks.FindAsync(id);
        if (walk == null)
        {
            return NotFound();
        }

        _context.Walks.Remove(walk);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool WalkExists(System.Guid? id)
    {
        return _context.Walks.Any(e => e.Id == id);
    }
}
