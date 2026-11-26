namespace DemoMVC.Models;

public class RolePermissionsViewModel
{
    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public List<ControllerPermissionsViewModel> Controllers { get; set; } = [];
}

public class ControllerPermissionsViewModel
{
    public string ControllerName { get; set; } = string.Empty;

    public List<ActionPermissionViewModel> Actions { get; set; } = [];
}

public class ActionPermissionViewModel
{
    public string ControllerName { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public string PermissionValue { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
