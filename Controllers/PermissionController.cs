using System.Security.Claims;
using DemoMVC.Helpers;
using DemoMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Controllers;

[Authorize(Roles = "Admin")]
public class PermissionController : Controller
{
    private const string PermissionClaimType = "Permission";
    private readonly RoleManager<IdentityRole> _roleManager;

    public PermissionController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return BadRequest("Mã Role không hợp lệ.");
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return NotFound("Không tìm thấy Role.");
        }

        var assignedPermissions = (await _roleManager.GetClaimsAsync(role))
            .Where(claim => claim.Type == PermissionClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);
        var controllers = ControllerActionDiscovery.Discover()
            .GroupBy(action => action.ControllerName, StringComparer.Ordinal)
            .Select(group => new ControllerPermissionsViewModel
            {
                ControllerName = group.Key,
                Actions = group.Select(action => new ActionPermissionViewModel
                {
                    ControllerName = action.ControllerName,
                    ActionName = action.ActionName,
                    PermissionValue = action.PermissionValue,
                    IsSelected = assignedPermissions.Contains(action.PermissionValue)
                }).ToList()
            })
            .ToList();

        return View(new RolePermissionsViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name ?? string.Empty,
            Controllers = controllers
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePermissions([FromBody] RolePermissionsViewModel? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.RoleId) || model.Controllers is null)
        {
            return BadRequest(new { success = false, message = "Dữ liệu phân quyền không hợp lệ." });
        }

        if (model.Controllers.Any(controller =>
                controller is null ||
                string.IsNullOrWhiteSpace(controller.ControllerName) ||
                controller.Actions is null ||
                controller.Actions.Any(action => action is null)))
        {
            return BadRequest(new { success = false, message = "Danh sách Action không hợp lệ." });
        }

        var role = await _roleManager.FindByIdAsync(model.RoleId);
        if (role is null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy Role cần cập nhật." });
        }

        var availablePermissions = ControllerActionDiscovery.Discover()
            .Select(action => action.PermissionValue)
            .ToHashSet(StringComparer.Ordinal);
        var selectedPermissions = model.Controllers
            .SelectMany(controller => controller.Actions)
            .Where(action => action.IsSelected)
            .Select(action => action.PermissionValue)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (selectedPermissions.Any(permission => !availablePermissions.Contains(permission)))
        {
            return BadRequest(new { success = false, message = "Danh sách có quyền Action không hợp lệ." });
        }

        var oldPermissionClaims = (await _roleManager.GetClaimsAsync(role))
            .Where(claim => claim.Type == PermissionClaimType)
            .ToList();
        foreach (var claim in oldPermissionClaims)
        {
            var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeResult.Succeeded)
            {
                return Json(new { success = false, message = GetIdentityErrors(removeResult) });
            }
        }

        foreach (var permission in selectedPermissions)
        {
            var addResult = await _roleManager.AddClaimAsync(
                role,
                new Claim(PermissionClaimType, permission));
            if (!addResult.Succeeded)
            {
                return Json(new { success = false, message = GetIdentityErrors(addResult) });
            }
        }

        return Json(new { success = true, message = "Cập nhật quyền Action thành công!" });
    }

    private static string GetIdentityErrors(IdentityResult result)
    {
        var message = string.Join(" ", result.Errors.Select(error => error.Description));
        return string.IsNullOrWhiteSpace(message)
            ? "Không thể cập nhật quyền Action."
            : message;
    }
}
