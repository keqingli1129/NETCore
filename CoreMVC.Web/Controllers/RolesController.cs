using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreMVC.Web.Controllers;

[Authorize]
public class RolesController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(userManager);

        _roleManager = roleManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Lists all roles.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        return View(roles);
    }

    /// <summary>
    /// Shows the create role form.
    /// </summary>
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Handles role creation.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            ModelState.AddModelError(string.Empty, "Role name is required.");
            return View();
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View();
    }

    /// <summary>
    /// Shows the edit role form.
    /// </summary>
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        return View(role);
    }

    /// <summary>
    /// Handles role update.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string roleName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            ModelState.AddModelError(string.Empty, "Role name is required.");
            return View(role);
        }

        role.Name = roleName.Trim();
        var result = await _roleManager.UpdateAsync(role);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(role);
    }

    /// <summary>
    /// Shows the delete confirmation page.
    /// </summary>
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        return View(role);
    }

    /// <summary>
    /// Handles role deletion.
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        await _roleManager.DeleteAsync(role);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Lists users in a role and users not in the role.
    /// </summary>
    public async Task<IActionResult> ManageUsers(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
            return NotFound();

        var users = _userManager.Users.ToList();
        var usersInRole = new List<IdentityUser>();
        var usersNotInRole = new List<IdentityUser>();
        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, role.Name!))
                usersInRole.Add(user);
            else
                usersNotInRole.Add(user);
        }

        var vm = new CoreMVC.Web.Models.ManageUsersViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name ?? string.Empty,
            UsersInRole = usersInRole,
            UsersNotInRole = usersNotInRole
        };
        return View(vm);
    }

    /// <summary>
    /// Assigns a user to a role.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUserToRole(string roleId, string userId)
    {
        if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(userId))
            return NotFound();

        var role = await _roleManager.FindByIdAsync(roleId);
        var user = await _userManager.FindByIdAsync(userId);
        if (role == null || user == null)
            return NotFound();

        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        }
        return RedirectToAction(nameof(ManageUsers), new { id = roleId });
    }

    /// <summary>
    /// Removes a user from a role.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUserFromRole(string roleId, string userId)
    {
        if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(userId))
            return NotFound();

        var role = await _roleManager.FindByIdAsync(roleId);
        var user = await _userManager.FindByIdAsync(userId);
        if (role == null || user == null)
            return NotFound();

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        }
        return RedirectToAction(nameof(ManageUsers), new { id = roleId });
    }
}
