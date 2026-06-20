using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreMVC.Contracts.Auth;
using CoreWebAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace CoreWebAPI.Tests;

public class AuthControllerTests
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var store = Substitute.For<IUserStore<IdentityUser>>();
        _userManager = Substitute.For<UserManager<IdentityUser>>(
            store, null, null, null, null, null, null, null, null);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnly1234567890",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpireMinutes"] = "60"
            })
            .Build();

        _controller = new AuthController(_userManager, _configuration);
    }

    [Fact]
    public async Task Register_WhenSuccessful_ReturnsOk()
    {
        var dto = new RegisterDto { Email = "test@example.com", Password = "P@ssw0rd!" };
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);

        var result = await _controller.Register(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_WhenFails_ReturnsBadRequest()
    {
        var dto = new RegisterDto { Email = "test@example.com", Password = "weak" };
        var errors = new[] { new IdentityError { Code = "PasswordTooShort", Description = "Too short" } };
        _userManager.CreateAsync(Arg.Any<IdentityUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(errors));

        var result = await _controller.Register(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "missing@example.com", Password = "P@ssw0rd!" };
        _userManager.FindByEmailAsync(dto.Email).Returns((IdentityUser?)null);

        var result = await _controller.Login(dto);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WhenPasswordInvalid_ReturnsUnauthorized()
    {
        var dto = new LoginDto { Email = "test@example.com", Password = "wrong" };
        var user = new IdentityUser { Id = "1", UserName = dto.Email, Email = dto.Email };
        _userManager.FindByEmailAsync(dto.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, dto.Password).Returns(false);

        var result = await _controller.Login(dto);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WhenValid_ReturnsOkWithToken()
    {
        var dto = new LoginDto { Email = "test@example.com", Password = "P@ssw0rd!" };
        var user = new IdentityUser { Id = "1", UserName = dto.Email, Email = dto.Email };
        _userManager.FindByEmailAsync(dto.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, dto.Password).Returns(true);

        var result = await _controller.Login(dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}
