using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTOs;

public record RegisterRequestDto
{
    [Required]
    [DataType(DataType.EmailAddress)]
    public required string Username { get; init; }

    [Required]
    [DataType(DataType.Password)]
    public required string Password { get; init; }

    public string[] Roles { get; init; } = [];
}
