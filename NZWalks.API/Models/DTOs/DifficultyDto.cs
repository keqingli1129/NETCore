namespace NZWalks.API.Models.DTOs;

public record DifficultyDto(Guid Id, string Name);

public record AddDifficultyRequestDto(string Name);

public record UpdateDifficultyRequestDto(string Name);
