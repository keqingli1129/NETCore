namespace NZWalks.API.Models.DTOs;

public record WalkDto(
    int Id,
    string Name,
    string Description,
    double LengthInKm,
    string? WalkImageUrl,
    int RegionId,
    int DifficultyId,
    RegionDto? Region,
    DifficultyDto? Difficulty);

public record AddWalkRequestDto(
    string Name,
    string Description,
    double LengthInKm,
    string? WalkImageUrl,
    int RegionId,
    int DifficultyId);

public record UpdateWalkRequestDto(
    string Name,
    string Description,
    double LengthInKm,
    string? WalkImageUrl,
    int RegionId,
    int DifficultyId);
