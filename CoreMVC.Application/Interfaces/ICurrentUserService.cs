using System;
using System.Collections.Generic;
using System.Text;

namespace CoreMVC.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Email { get; }
        string? DisplayName { get; }
        bool IsAuthenticated { get; }
        IList<string> Roles { get; }
        IList<string> Permissions { get; }
        bool HasPermission(string permission);
        bool HasRole(string role);
    }
}
