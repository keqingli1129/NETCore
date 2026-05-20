using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreMVC.Web.Controllers;
using CoreMVC.Web.Models;
using Xunit;

namespace CoreMVC.Web.Tests;

public class RolesControllerTests
{
    private RoleManager<IdentityRole> CreateRoleManager(IEnumerable<IdentityRole> roles)
    {
        var store = A.Fake<Microsoft.AspNetCore.Identity.IQueryableRoleStore<IdentityRole>>();
        var roleValidators = new List<IRoleValidator<IdentityRole>>();
        var lookupNormalizer = A.Fake<Microsoft.AspNetCore.Identity.ILookupNormalizer>();
        var errors = A.Fake<Microsoft.AspNetCore.Identity.IdentityErrorDescriber>();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<RoleManager<IdentityRole>>>();

        var roleManager = new RoleManager<IdentityRole>(store, roleValidators, lookupNormalizer, errors, logger);

        // Fake FindById and Roles enumerable via store
        A.CallTo(() => store.FindByIdAsync(A<string>._, default)).ReturnsLazily((string id, System.Threading.CancellationToken _) =>
            Task.FromResult(roles.FirstOrDefault(r => r.Id == id)));
        A.CallTo(() => ((Microsoft.AspNetCore.Identity.IQueryableRoleStore<IdentityRole>)store).Roles).Returns(new TestAsyncEnumerable<IdentityRole>(roles));

        return roleManager;
    }

    private UserManager<IdentityUser> CreateUserManager(IEnumerable<IdentityUser> users)
    {
        var store = A.Fake<IUserStore<IdentityUser>>();
        var userValidators = new List<IUserValidator<IdentityUser>>();
        var pwdValidators = new List<IPasswordValidator<IdentityUser>>();
        var keyNormalizer = A.Fake<Microsoft.AspNetCore.Identity.ILookupNormalizer>();
        var errors = A.Fake<Microsoft.AspNetCore.Identity.IdentityErrorDescriber>();
        var logger = A.Fake<Microsoft.Extensions.Logging.ILogger<UserManager<IdentityUser>>>();

        var userManager = new UserManager<IdentityUser>(store, null, null, userValidators, pwdValidators, keyNormalizer, errors, null, logger);

        A.CallTo(() => store.FindByIdAsync(A<string>._, default)).ReturnsLazily((string id, System.Threading.CancellationToken _) =>
            Task.FromResult(users.FirstOrDefault(u => u.Id == id)));

        // Provide Users via extension by making user manager return the list when Users property is accessed via store if needed in tests
        return userManager;
    }

    [Fact]
    public async Task Index_ReturnsViewWithRoles()
    {
        // Arrange
        var roles = new[] { new IdentityRole("Admin"), new IdentityRole("User") };
        var roleManager = CreateRoleManager(roles);

        var userStore = A.Fake<IUserStore<IdentityUser>>();
        var userManager = A.Fake<UserManager<IdentityUser>>(x => x.WithArgumentsForConstructor(() =>
            new UserManager<IdentityUser>(userStore, null, null, null, null, null, null, null, null)));

        var controller = new RolesController(roleManager, userManager);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<IdentityRole>>().Subject;
        model.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Admin", "User" });
    }

    [Fact]
    public async Task ManageUsers_ReturnsViewWithUsersInAndNotInRole()
    {
        // Arrange
        var role = new IdentityRole("Admin") { Id = "role-1" };
        var roles = new[] { role };
        var roleManager = CreateRoleManager(roles);

        var user1 = new IdentityUser("user1") { Id = "u1" };
        var user2 = new IdentityUser("user2") { Id = "u2" };
        var users = new[] { user1, user2 };

        var userStore = A.Fake<IUserStore<IdentityUser>>();
        var userManager = A.Fake<UserManager<IdentityUser>>(x => x.WithArgumentsForConstructor(() =>
            new UserManager<IdentityUser>(userStore, null, null, null, null, null, null, null, null)));

        A.CallTo(() => userManager.Users).Returns(users.AsQueryable());
        A.CallTo(() => userManager.IsInRoleAsync(user1, "Admin")).Returns(Task.FromResult(true));
        A.CallTo(() => userManager.IsInRoleAsync(user2, "Admin")).Returns(Task.FromResult(false));

        var controller = new RolesController(roleManager, userManager);

        // Act
        var result = await controller.ManageUsers(role.Id);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<ManageUsersViewModel>().Subject;

        model.RoleId.Should().Be(role.Id);
        model.RoleName.Should().Be(role.Name);
        model.UsersInRole.Select(u => u.Id).Should().BeEquivalentTo(new[] { "u1" });
        model.UsersNotInRole.Select(u => u.Id).Should().BeEquivalentTo(new[] { "u2" });
    }
}
