using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;
using NZWalks.API.Repositories;

[Route("api/[controller]")]
[ApiController]
public class DifficultiesController : ControllerBase
{
    private readonly IDifficultyRepository _difficultyRepository;
    private readonly IMapper _mapper;

    public DifficultiesController(IDifficultyRepository difficultyRepository, IMapper mapper)
    {
        _difficultyRepository = difficultyRepository;
        _mapper = mapper;
    }

    // GET: api/Difficulties
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DifficultyDto>>> GetDifficulty()
    {
        var difficulties = await _difficultyRepository.GetAllAsync();
        return Ok(_mapper.Map<List<DifficultyDto>>(difficulties));
    }

    // GET: api/Difficulties/5
    [HttpGet("{id}")]
    public async Task<ActionResult<DifficultyDto>> GetDifficulty(int id)
    {
        var difficulty = await _difficultyRepository.GetAsync(id);

        if (difficulty == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<DifficultyDto>(difficulty));
    }

    // PUT: api/Difficulties/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutDifficulty(int id, UpdateDifficultyRequestDto updateDifficultyRequestDto)
    {
        var difficulty = _mapper.Map<Difficulty>(updateDifficultyRequestDto);
        var updatedDifficulty = await _difficultyRepository.UpdateAsync(id, difficulty);

        if (updatedDifficulty == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<DifficultyDto>(updatedDifficulty));
    }

    // POST: api/Difficulties
    [HttpPost]
    public async Task<ActionResult<DifficultyDto>> PostDifficulty(AddDifficultyRequestDto addDifficultyRequestDto)
    {
        var difficulty = _mapper.Map<Difficulty>(addDifficultyRequestDto);
        difficulty = await _difficultyRepository.AddAsync(difficulty);

        var difficultyDto = _mapper.Map<DifficultyDto>(difficulty);
        return CreatedAtAction(nameof(GetDifficulty), new { id = difficultyDto.Id }, difficultyDto);
    }

    // DELETE: api/Difficulties/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDifficulty(int id)
    {
        if (await _difficultyRepository.HasWalksAsync(id))
        {
            return Problem(
                title: "Difficulty is in use",
                detail: $"Difficulty {id} cannot be deleted because one or more walks reference it. Delete those walks first.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var difficulty = await _difficultyRepository.DeleteAsync(id);

        if (difficulty == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<DifficultyDto>(difficulty));
    }
}
