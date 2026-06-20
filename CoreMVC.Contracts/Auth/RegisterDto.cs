namespace CoreMVC.Contracts.Auth;

public record RegisterDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
