namespace DemoMVC.Models;

public class UserRolesViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string? Email { get; set; }

    public List<UserRoleItemViewModel> Roles { get; set; } = [];
}

public class UserRoleItemViewModel
{
    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
