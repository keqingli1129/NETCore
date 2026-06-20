using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreMVC.Contracts.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CoreWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };
        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "User registered successfully." });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private string GenerateJwtToken(IdentityUser user)
    {
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expireMinutes = int.TryParse(configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 60;

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
