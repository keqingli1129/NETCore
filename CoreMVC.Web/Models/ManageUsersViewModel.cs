using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace CoreMVC.Web.Models;

public class ManageUsersViewModel
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<IdentityUser> UsersInRole { get; set; } = new List<IdentityUser>();
    public List<IdentityUser> UsersNotInRole { get; set; } = new List<IdentityUser>();
}
