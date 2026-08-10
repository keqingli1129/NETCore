using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;
using NZWalks.API.Repositories;
using Microsoft.AspNetCore.Hosting;

[Route("api/[controller]")]
[ApiController]
public class RegionsController : ControllerBase
{
    private readonly IRegionRepository _regionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RegionsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public RegionsController(IRegionRepository regionRepository, IMapper mapper, ILogger<RegionsController> logger, IWebHostEnvironment environment)
    {
        _regionRepository = regionRepository;
        _mapper = mapper;
        _logger = logger;
        _environment = environment;
    }

    private async Task<string?> SaveImageAsync(IFormFile? image)
    {
        if (image is null || image.Length == 0)
            return null;

        var webRootPath = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "images", "regions");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/images/regions/{fileName}";
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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PutRegion(int id, [FromForm] UpdateRegionRequestDto updateRegionRequestDto)
    {
        _logger.LogInformation("PutRegion action invoked for id {RegionId}", id);
        var region = _mapper.Map<Region>(updateRegionRequestDto);
        region.RegionImageUrl = await SaveImageAsync(updateRegionRequestDto.Image);
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
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<RegionDto>> PostRegion([FromForm] AddRegionRequestDto addRegionRequestDto)
    {
        _logger.LogInformation("PostRegion action invoked");
        var region = _mapper.Map<Region>(addRegionRequestDto);
        region.RegionImageUrl = await SaveImageAsync(addRegionRequestDto.Image);
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
