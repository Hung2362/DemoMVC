using DemoMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

[Authorize(Roles = "Admin")]
public class UserRoleController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserRoleController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync();
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync();
        var userRoleModels = new List<UserRolesViewModel>(users.Count);

        foreach (var user in users)
        {
            var userRoleNames = new HashSet<string>(
                await _userManager.GetRolesAsync(user),
                StringComparer.OrdinalIgnoreCase);

            userRoleModels.Add(new UserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.Select(role => new UserRoleItemViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    IsSelected = role.Name is not null && userRoleNames.Contains(role.Name)
                }).ToList()
            });
        }

        return View(userRoleModels);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserRoles(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { success = false, message = "Mã người dùng không hợp lệ." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy người dùng." });
        }

        var userRoleNames = new HashSet<string>(
            await _userManager.GetRolesAsync(user),
            StringComparer.OrdinalIgnoreCase);
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => new UserRoleItemViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                IsSelected = role.Name != null && userRoleNames.Contains(role.Name)
            })
            .ToListAsync();

        return Json(new UserRolesViewModel
        {
            UserId = user.Id,
            Email = user.Email,
            Roles = roles
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRoles([FromBody] UserRolesViewModel? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.UserId) || model.Roles is null)
        {
            return BadRequest(new { success = false, message = "Dữ liệu phân quyền không hợp lệ." });
        }

        if (model.Roles.Any(role => role is null || string.IsNullOrWhiteSpace(role.RoleId)))
        {
            return BadRequest(new { success = false, message = "Danh sách Role không hợp lệ." });
        }

        var roleIds = model.Roles.Select(role => role.RoleId).ToList();
        if (roleIds.Distinct(StringComparer.Ordinal).Count() != roleIds.Count)
        {
            return BadRequest(new { success = false, message = "Danh sách Role bị trùng." });
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy người dùng cần cập nhật." });
        }

        var rolesById = await _roleManager.Roles
            .ToDictionaryAsync(role => role.Id);
        if (roleIds.Any(roleId => !rolesById.ContainsKey(roleId)))
        {
            return BadRequest(new { success = false, message = "Danh sách có Role không còn tồn tại." });
        }

        var selectedRoleNames = model.Roles
            .Where(role => role.IsSelected)
            .Select(role => rolesById[role.RoleId].Name)
            .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
            .Select(roleName => roleName!)
            .ToList();
        var currentRoleNames = await _userManager.GetRolesAsync(user);
        var rolesToAdd = selectedRoleNames
            .Except(currentRoleNames, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var rolesToRemove = currentRoleNames
            .Except(selectedRoleNames, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return Json(new { success = false, message = GetIdentityErrors(removeResult) });
            }
        }

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return Json(new { success = false, message = GetIdentityErrors(addResult) });
            }
        }

        return Json(new { success = true, message = "Cập nhật phân quyền thành công!" });
    }

    private static string GetIdentityErrors(IdentityResult result)
    {
        var message = string.Join(" ", result.Errors.Select(error => error.Description));
        return string.IsNullOrWhiteSpace(message)
            ? "Không thể cập nhật phân quyền người dùng."
            : message;
    }
}
