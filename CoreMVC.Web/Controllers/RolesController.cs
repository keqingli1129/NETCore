using CoreMVC.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreMVC.Web.Controllers;

[Authorize(Roles = "Admin")]
public class RolesController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ITokenCacheService? _tokenCache;

    // Constructor used by the application DI (includes token cache)
    public RolesController(ITokenCacheService tokenCache, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(userManager);

        _roleManager = roleManager;
        _userManager = userManager;
        _tokenCache = tokenCache;
    }

    //// Convenience overload for tests that don't provide an ITokenCacheService
    //public RolesController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    //{
    //    ArgumentNullException.ThrowIfNull(roleManager);
    //    ArgumentNullException.ThrowIfNull(userManager);

    //    _roleManager = roleManager;
    //    _userManager = userManager;
    //    _tokenCache = null;
    //}

    /// <summary>
    /// Lists all roles.
    /// </summary>
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
    {
        //var userId = _userManager.GetUserId(User)!;
        //var accessToken = await _tokenCache.GetAccessTokenAsync(userId);

        //if (string.IsNullOrEmpty(accessToken))
        //{
        //    // Token expired or user logged in via a different provider
        //    // Redirect to re-authenticate or handle gracefully
        //    // Force re-authentication — signs out and sends to login
        //    //await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        //    return RedirectToPage("/Account/Login", new { area = "Identity" });
        //}
        // Use the token — e.g. call Microsoft Graph
        // var graphClient = new HttpClient();
        // graphClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", accessToken);
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _roleManager.Roles.OrderBy(r => r.Name);
        var totalCount = await query.CountAsync();
        var roles = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["PageNumber"] = pageNumber;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalPages"] = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewData["TotalCount"] = totalCount;

        //ViewBag.AccessToken = accessToken; // Pass token to view if needed for API calls
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
