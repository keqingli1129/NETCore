using CoreMVC.Application.Interfaces;
using CoreMVC.Domain.Authorization;
using CoreMVC.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreMVC.Web.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ICurrentUserService _currentUser;
        private readonly ITokenCacheService _tokenCache;

        public UsersController(ICurrentUserService currentUser, ITokenCacheService tokenCache)
        {
            _currentUser = currentUser;
            _tokenCache = tokenCache;
        }

        // Via policy attribute
        [Authorize(Policy = Permissions.Users.View)]
        public IActionResult Index() => View();

        // Via injected service (programmatic check)
        public IActionResult Settings()
        {
            if (!_currentUser.HasPermission(Permissions.Users.Edit))
                return Forbid();

            return View();
        }

        // Enrich claims from access token (e.g. Entra ID roles)
        public async Task<IActionResult> Dashboard()
        {
            var userId = _currentUser.UserId!;
            var token = await _tokenCache.GetAccessTokenAsync(userId);

            if (!string.IsNullOrEmpty(token))
            {
                var roles = AccessTokenHandler.ExtractRoles(token);
                // use roles for display/logic — policy enforcement is handled above
            }

            return View();
        }
    }
}
