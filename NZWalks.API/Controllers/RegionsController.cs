using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;
using NZWalks.API.Repositories;

[Route("api/[controller]")]
[ApiController]
public class RegionsController : ControllerBase
{
    private readonly IRegionRepository _regionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RegionsController> _logger;

    public RegionsController(IRegionRepository regionRepository, IMapper mapper, ILogger<RegionsController> logger)
    {
        _regionRepository = regionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    // GET: api/Regions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RegionDto>>> GetRegion()
    {
        _logger.LogInformation("GetAllRegions action invoked");
        var regions = await _regionRepository.GetAllAsync();
        _logger.LogInformation("GetAllRegions returned {Count} regions", regions.Count());
        return Ok(_mapper.Map<List<RegionDto>>(regions));
    }

    // GET: api/Regions/5
    [HttpGet("{id}")]
    public async Task<ActionResult<RegionDto>> GetRegion(int id)
    {
        _logger.LogInformation("GetRegion action invoked for id {RegionId}", id);
        var region = await _regionRepository.GetAsync(id);

        if (region == null)
        {
            _logger.LogWarning("Region with id {RegionId} not found", id);
            return NotFound();
        }

        return Ok(_mapper.Map<RegionDto>(region));
    }

    // PUT: api/Regions/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRegion(int id, UpdateRegionRequestDto updateRegionRequestDto)
    {
        _logger.LogInformation("PutRegion action invoked for id {RegionId}", id);
        var region = _mapper.Map<Region>(updateRegionRequestDto);
        var updatedRegion = await _regionRepository.UpdateAsync(id, region);

        if (updatedRegion == null)
        {
            _logger.LogWarning("Region with id {RegionId} not found for update", id);
            return NotFound();
        }

        return Ok(_mapper.Map<RegionDto>(updatedRegion));
    }

    // POST: api/Regions
    [HttpPost]
    public async Task<ActionResult<RegionDto>> PostRegion(AddRegionRequestDto addRegionRequestDto)
    {
        _logger.LogInformation("PostRegion action invoked");
        var region = _mapper.Map<Region>(addRegionRequestDto);
        region = await _regionRepository.AddAsync(region);

        var regionDto = _mapper.Map<RegionDto>(region);
        return CreatedAtAction(nameof(GetRegion), new { id = regionDto.Id }, regionDto);
    }

    // DELETE: api/Regions/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        _logger.LogInformation("DeleteRegion action invoked for id {RegionId}", id);
        if (await _regionRepository.HasWalksAsync(id))
        {
            return Problem(
                title: "Region is in use",
                detail: $"Region {id} cannot be deleted because one or more walks reference it. Delete those walks first.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var region = await _regionRepository.DeleteAsync(id);

        if (region == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<RegionDto>(region));
    }
}
