using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

[Authorize(Roles = "Admin")]
public class RoleController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => new { role.Id, role.Name })
            .ToListAsync();

        return Json(roles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? roleName)
    {
        var validationMessage = ValidateRoleName(roleName);
        if (validationMessage is not null)
        {
            return Json(new { success = false, message = validationMessage });
        }

        roleName = roleName!.Trim();
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return Json(new { success = false, message = "Tên Role này đã tồn tại." });
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            return Json(new { success = false, message = GetIdentityErrors(result) });
        }

        return Json(new { success = true, message = "Thêm Role thành công!" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? id, string? roleName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Json(new { success = false, message = "Mã Role không hợp lệ." });
        }

        var validationMessage = ValidateRoleName(roleName);
        if (validationMessage is not null)
        {
            return Json(new { success = false, message = validationMessage });
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return Json(new { success = false, message = "Không tìm thấy Role cần cập nhật." });
        }

        roleName = roleName!.Trim();
        var existingRole = await _roleManager.FindByNameAsync(roleName);
        if (existingRole is not null && existingRole.Id != role.Id)
        {
            return Json(new { success = false, message = "Tên Role này đã tồn tại." });
        }

        role.Name = roleName;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return Json(new { success = false, message = GetIdentityErrors(result) });
        }

        return Json(new { success = true, message = "Cập nhật Role thành công!" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Json(new { success = false, message = "Mã Role không hợp lệ." });
        }

        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return Json(new { success = false, message = "Không tìm thấy Role cần xóa." });
        }

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role.Name, "User", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "Không thể xóa Role hệ thống Admin hoặc User." });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return Json(new { success = false, message = GetIdentityErrors(result) });
        }

        return Json(new { success = true, message = "Xóa Role thành công!" });
    }

    private static string? ValidateRoleName(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return "Vui lòng nhập tên Role.";
        }

        if (roleName.Trim().Length > 256)
        {
            return "Tên Role không được vượt quá 256 ký tự.";
        }

        return null;
    }

    private static string GetIdentityErrors(IdentityResult result)
    {
        var message = string.Join(" ", result.Errors.Select(error => error.Description));
        return string.IsNullOrWhiteSpace(message)
            ? "Không thể hoàn tất thao tác với Role."
            : message;
    }
}
