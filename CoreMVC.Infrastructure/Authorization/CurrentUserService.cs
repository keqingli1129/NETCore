using CoreMVC.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace CoreMVC.Infrastructure.Authorization
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly ClaimsPrincipal? _user;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _user = httpContextAccessor.HttpContext?.User;
        }

        public string? UserId => _user?.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? Email => _user?.FindFirstValue(ClaimTypes.Email)
                                           ?? _user?.FindFirstValue("preferred_username");
        public string? DisplayName => _user?.FindFirstValue("name")
                                           ?? _user?.FindFirstValue(ClaimTypes.Name);
        public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

        public IList<string> Roles =>
            _user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            ?? new List<string>();

        public IList<string> Permissions =>
            _user?.FindAll("permission").Select(c => c.Value).ToList()
            ?? new List<string>();

        public bool HasPermission(string permission) =>
            Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

        public bool HasRole(string role) =>
            Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
