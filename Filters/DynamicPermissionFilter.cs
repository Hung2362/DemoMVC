using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DemoMVC.Filters;

public sealed class DynamicPermissionFilter : IAsyncAuthorizationFilter
{
    private const string PermissionClaimType = "Permission";
    private static readonly HashSet<string> ExcludedControllers = new(StringComparer.Ordinal)
    {
        "Home",
        "Role",
        "UserRole",
        "Permission"
    };

    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DynamicPermissionFilter(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (AllowsAnonymous(context) ||
            IsIdentityArea(context) ||
            context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor ||
            ExcludedControllers.Contains(actionDescriptor.ControllerName))
        {
            return;
        }

        var principal = context.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            context.Result = IsAjaxRequest(context.HttpContext.Request)
                ? new StatusCodeResult(StatusCodes.Status401Unauthorized)
                : new ChallengeResult();
            return;
        }

        var user = await _userManager.GetUserAsync(principal);
        if (user is null)
        {
            context.Result = IsAjaxRequest(context.HttpContext.Request)
                ? new StatusCodeResult(StatusCodes.Status401Unauthorized)
                : new ChallengeResult();
            return;
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return;
        }

        var permissionValue = $"{actionDescriptor.ControllerName}.{actionDescriptor.ActionName}";
        var roleNames = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            if (claims.Any(claim =>
                    claim.Type == PermissionClaimType &&
                    claim.Value == permissionValue))
            {
                return;
            }
        }

        context.Result = IsAjaxRequest(context.HttpContext.Request)
            ? new StatusCodeResult(StatusCodes.Status403Forbidden)
            : new ForbidResult();
    }

    private static bool AllowsAnonymous(AuthorizationFilterContext context)
    {
        return context.Filters.OfType<IAllowAnonymousFilter>().Any() ||
            context.ActionDescriptor.EndpointMetadata?.OfType<IAllowAnonymous>().Any() == true;
    }

    private static bool IsIdentityArea(AuthorizationFilterContext context)
    {
        return context.RouteData.Values.TryGetValue("area", out var area) &&
            string.Equals(area?.ToString(), "Identity", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return request.Headers["X-Requested-With"]
            .ToString()
            .Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }
}
