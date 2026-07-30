using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;

[Route("api/[controller]")]
[ApiController]
public class DifficultiesController : ControllerBase
{
    private readonly MVCNetNZWalksContext _context;
    private readonly IMapper _mapper;

    public DifficultiesController(MVCNetNZWalksContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET: api/Difficulty
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DifficultyDto>>> GetDifficulty()
    {
        var difficulties = await _context.Difficulties.ToListAsync();
        return Ok(_mapper.Map<List<DifficultyDto>>(difficulties));
    }

    // GET: api/Difficulty/5
    [HttpGet("{id}")]
    public async Task<ActionResult<DifficultyDto>> GetDifficulty(int id)
    {
        var difficulty = await _context.Difficulties.FindAsync(id);

        if (difficulty == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<DifficultyDto>(difficulty));
    }

    // PUT: api/Difficulty/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutDifficulty(int id, UpdateDifficultyRequestDto updateDifficultyRequestDto)
    {
        var difficulty = await _context.Difficulties.FindAsync(id);

        if (difficulty == null)
        {
            return NotFound();
        }

        _mapper.Map(updateDifficultyRequestDto, difficulty);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DifficultyExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return Ok(_mapper.Map<DifficultyDto>(difficulty));
    }

    // POST: api/Difficulty
    [HttpPost]
    public async Task<ActionResult<DifficultyDto>> PostDifficulty(AddDifficultyRequestDto addDifficultyRequestDto)
    {
        var difficulty = _mapper.Map<Difficulty>(addDifficultyRequestDto);

        _context.Difficulties.Add(difficulty);
        await _context.SaveChangesAsync();

        var difficultyDto = _mapper.Map<DifficultyDto>(difficulty);
        return CreatedAtAction(nameof(GetDifficulty), new { id = difficultyDto.Id }, difficultyDto);
    }

    // DELETE: api/Difficulty/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDifficulty(int id)
    {
        var difficulty = await _context.Difficulties.FindAsync(id);
        if (difficulty == null)
        {
            return NotFound();
        }

        _context.Difficulties.Remove(difficulty);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<DifficultyDto>(difficulty));
    }

    private bool DifficultyExists(int id)
    {
        return _context.Difficulties.Any(e => e.Id == id);
    }
}
