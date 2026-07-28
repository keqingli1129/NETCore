using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;

[Route("api/[controller]")]
[ApiController]
public class WalksController : ControllerBase
{
    private readonly MVCNetNZWalksContext _context;
    private readonly IMapper _mapper;

    public WalksController(MVCNetNZWalksContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET: api/Walk
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WalkDto>>> GetWalk()
    {
        var walks = await _context.Walks
            .Include(w => w.Region)
            .Include(w => w.Difficulty)
            .ToListAsync();

        return Ok(_mapper.Map<List<WalkDto>>(walks));
    }

    // GET: api/Walk/5
    [HttpGet("{id}")]
    public async Task<ActionResult<WalkDto>> GetWalk(Guid id)
    {
        var walk = await _context.Walks
            .Include(w => w.Region)
            .Include(w => w.Difficulty)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (walk == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<WalkDto>(walk));
    }

    // PUT: api/Walk/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutWalk(Guid id, UpdateWalkRequestDto updateWalkRequestDto)
    {
        var walk = await _context.Walks.FindAsync(id);

        if (walk == null)
        {
            return NotFound();
        }

        _mapper.Map(updateWalkRequestDto, walk);

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

        walk = await _context.Walks
            .Include(w => w.Region)
            .Include(w => w.Difficulty)
            .FirstOrDefaultAsync(w => w.Id == id);

        return Ok(_mapper.Map<WalkDto>(walk));
    }

    // POST: api/Walk
    [HttpPost]
    public async Task<ActionResult<WalkDto>> PostWalk(AddWalkRequestDto addWalkRequestDto)
    {
        var walk = _mapper.Map<Walk>(addWalkRequestDto);

        _context.Walks.Add(walk);
        await _context.SaveChangesAsync();

        walk = await _context.Walks
            .Include(w => w.Region)
            .Include(w => w.Difficulty)
            .FirstOrDefaultAsync(w => w.Id == walk.Id);

        var walkDto = _mapper.Map<WalkDto>(walk);
        return CreatedAtAction(nameof(GetWalk), new { id = walkDto.Id }, walkDto);
    }

    // DELETE: api/Walk/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWalk(Guid id)
    {
        var walk = await _context.Walks.FindAsync(id);
        if (walk == null)
        {
            return NotFound();
        }

        _context.Walks.Remove(walk);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<WalkDto>(walk));
    }

    private bool WalkExists(Guid id)
    {
        return _context.Walks.Any(e => e.Id == id);
    }
}
