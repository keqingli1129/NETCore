using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models.DTOs;
using NZWalks.API.Repositories;

namespace NZWalks.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ITokenRepository _tokenRepository;

    public AuthController(UserManager<IdentityUser> userManager, ITokenRepository tokenRepository)
    {
        _userManager = userManager;
        _tokenRepository = tokenRepository;
    }

    /// <summary>
    /// Registers a new user with the specified roles.
    /// </summary>
    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var identityUser = new IdentityUser
        {
            UserName = request.Username,
            Email = request.Username
        };

        var result = await _userManager.CreateAsync(identityUser, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        if (request.Roles.Length > 0)
        {
            var roleResult = await _userManager.AddToRolesAsync(identityUser, request.Roles);
            if (!roleResult.Succeeded)
            {
                return BadRequest(roleResult.Errors);
            }
        }

        return Ok("User was registered successfully.");
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Username);

        if (user is null)
        {
            return BadRequest("Username or password is incorrect.");
        }

        var checkPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!checkPassword)
        {
            return BadRequest("Username or password is incorrect.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var jwtToken = _tokenRepository.CreateJwtToken(user, roles.ToList());

        return Ok(new LoginResponseDto(jwtToken));
    }
}
