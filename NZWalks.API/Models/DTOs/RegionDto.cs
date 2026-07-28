namespace NZWalks.API.Models.DTOs;

public record RegionDto(Guid Id, string Code, string Name, string? RegionImageUrl);

public record AddRegionRequestDto(string Code, string Name, string? RegionImageUrl);

public record UpdateRegionRequestDto(string Code, string Name, string? RegionImageUrl);
