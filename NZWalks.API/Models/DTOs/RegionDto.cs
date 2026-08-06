using Microsoft.AspNetCore.Http;

namespace NZWalks.API.Models.DTOs;

public record RegionDto(int Id, string Code, string Name, string? RegionImageUrl);

public class AddRegionRequestDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public IFormFile? Image { get; set; }
}

public class UpdateRegionRequestDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public IFormFile? Image { get; set; }
}
