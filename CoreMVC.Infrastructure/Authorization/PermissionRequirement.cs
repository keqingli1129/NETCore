using Microsoft.AspNetCore.Authorization;

namespace CoreMVC.Infrastructure.Authorization
{
    public record PermissionRequirement(string Permission) : IAuthorizationRequirement;

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var hasClaim = context.User
                .FindAll("permission")
                .Any(c => c.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

}