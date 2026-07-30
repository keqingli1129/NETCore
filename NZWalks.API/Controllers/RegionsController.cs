using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;

[Route("api/[controller]")]
[ApiController]
public class RegionsController : ControllerBase
{
    private readonly MVCNetNZWalksContext _context;
    private readonly IMapper _mapper;

    public RegionsController(MVCNetNZWalksContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET: api/Region
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegionDto>>> GetRegion()
    {
        var regions = await _context.Regions.ToListAsync();
        return Ok(_mapper.Map<List<RegionDto>>(regions));
    }

    // GET: api/Region/5
    [HttpGet("{id}")]
    public async Task<ActionResult<RegionDto>> GetRegion(int id)
    {
        var region = await _context.Regions.FindAsync(id);

        if (region == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<RegionDto>(region));
    }

    // PUT: api/Region/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRegion(int id, UpdateRegionRequestDto updateRegionRequestDto)
    {
        var region = await _context.Regions.FindAsync(id);

        if (region == null)
        {
            return NotFound();
        }

        _mapper.Map(updateRegionRequestDto, region);

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

        return Ok(_mapper.Map<RegionDto>(region));
    }

    // POST: api/Region
    [HttpPost]
    public async Task<ActionResult<RegionDto>> PostRegion(AddRegionRequestDto addRegionRequestDto)
    {
        var region = _mapper.Map<Region>(addRegionRequestDto);

        _context.Regions.Add(region);
        await _context.SaveChangesAsync();

        var regionDto = _mapper.Map<RegionDto>(region);
        return CreatedAtAction(nameof(GetRegion), new { id = regionDto.Id }, regionDto);
    }

    // DELETE: api/Region/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null)
        {
            return NotFound();
        }

        _context.Regions.Remove(region);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<RegionDto>(region));
    }

    private bool RegionExists(int id)
    {
        return _context.Regions.Any(e => e.Id == id);
    }
}
