using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;
using NZWalks.API.Repositories;

[Route("api/[controller]")]
[ApiController]
public class WalksController : ControllerBase
{
    private readonly IWalkRepository _walkRepository;
    private readonly IMapper _mapper;

    public WalksController(IWalkRepository walkRepository, IMapper mapper)
    {
        _walkRepository = walkRepository;
        _mapper = mapper;
    }

    // GET: api/Walks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WalkDto>>> GetWalk()
    {
        var walks = await _walkRepository.GetAllAsync();
        return Ok(_mapper.Map<List<WalkDto>>(walks));
    }

    // GET: api/Walks/5
    [HttpGet("{id}")]
    public async Task<ActionResult<WalkDto>> GetWalk(int id)
    {
        var walk = await _walkRepository.GetAsync(id);

        if (walk == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<WalkDto>(walk));
    }

    // PUT: api/Walks/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutWalk(int id, UpdateWalkRequestDto updateWalkRequestDto)
    {
        var walk = _mapper.Map<Walk>(updateWalkRequestDto);
        var updatedWalk = await _walkRepository.UpdateAsync(id, walk);

        if (updatedWalk == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<WalkDto>(updatedWalk));
    }

    // POST: api/Walks
    [HttpPost]
    public async Task<ActionResult<WalkDto>> PostWalk(AddWalkRequestDto addWalkRequestDto)
    {
        var walk = _mapper.Map<Walk>(addWalkRequestDto);
        walk = await _walkRepository.AddAsync(walk);

        var walkDto = _mapper.Map<WalkDto>(walk);
        return CreatedAtAction(nameof(GetWalk), new { id = walkDto.Id }, walkDto);
    }

    // DELETE: api/Walks/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWalk(int id)
    {
        var walk = await _walkRepository.DeleteAsync(id);

        if (walk == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<WalkDto>(walk));
    }
}
