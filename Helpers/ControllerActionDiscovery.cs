using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Helpers;

public sealed record DiscoveredControllerAction(string ControllerName, string ActionName)
{
    public string PermissionValue => $"{ControllerName}.{ActionName}";
}

public static class ControllerActionDiscovery
{
    private static readonly HashSet<string> ExcludedControllers = new(StringComparer.Ordinal)
    {
        "RoleController",
        "UserRoleController",
        "PermissionController"
    };

    public static IReadOnlyList<DiscoveredControllerAction> Discover()
    {
        return typeof(ControllerActionDiscovery).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsGenericType &&
                typeof(Controller).IsAssignableFrom(type) &&
                !ExcludedControllers.Contains(type.Name))
            .SelectMany(type =>
            {
                var controllerName = type.Name.EndsWith("Controller", StringComparison.Ordinal)
                    ? type.Name[..^"Controller".Length]
                    : type.Name;

                return type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(method =>
                        method.DeclaringType?.Assembly == type.Assembly &&
                        !method.IsSpecialName &&
                        !method.IsGenericMethod &&
                        !method.IsDefined(typeof(NonActionAttribute), inherit: true))
                    .Select(method => new DiscoveredControllerAction(
                        controllerName,
                        method.GetCustomAttribute<ActionNameAttribute>(inherit: true)?.Name ?? method.Name));
            })
            .DistinctBy(action => action.PermissionValue, StringComparer.Ordinal)
            .OrderBy(action => action.ControllerName, StringComparer.Ordinal)
            .ThenBy(action => action.ActionName, StringComparer.Ordinal)
            .ToList();
    }
}
