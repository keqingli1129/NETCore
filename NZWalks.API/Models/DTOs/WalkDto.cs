namespace NZWalks.API.Models.DTOs;

public record WalkDto(
    Guid Id,
    string Name,
    string Description,
    double LengthInKm,
    string? WalkImageUrl,
    Guid RegionId,
    Guid DifficultyId,
    RegionDto? Region,
    DifficultyDto? Difficulty);

public record AddWalkRequestDto(
    string Name,
    string Description,
    double LengthInKm,
    string? WalkImageUrl,
    Guid RegionId,
    Guid DifficultyId);

public record UpdateWalkRequestDto(
    string Name,
    string Description,
    double LengthInKm,
    string? WalkImageUrl,
    Guid RegionId,
    Guid DifficultyId);
