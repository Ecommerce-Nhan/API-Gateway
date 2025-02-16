using Microsoft.Extensions.Caching.Distributed;
using Ocelot.Authorization;
using Ocelot.Middleware;
using System.Security.Claims;
using System.Text.Json;

namespace APIGateway.Middleware;

public class OcelotAuthorizationMiddleware
{
    private static readonly Dictionary<string, string> MethodToPermission = new()
    {
        { "GET", "View" },
        { "POST", "Create" },
        { "PUT", "Edit" },
        { "DELETE", "Delete" }
    };

    public static async Task Handle(HttpContext context, Func<Task> next)
    {
        var cache = context.RequestServices.GetRequiredService<IDistributedCache>();
        var user = context.User;

        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Items.SetError(new UnauthenticatedError("Unauthorized"));
            return;
        }

        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!userRoles.Any())
        {
            await next.Invoke();
            return;
        }

        var method = context.Request.Method.ToUpper();
        var path = context.Request.Path.Value?.Trim('/') ?? string.Empty;
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resourceName = pathSegments.FirstOrDefault();
        resourceName = string.IsNullOrEmpty(resourceName)
                       ? "Unknown"
                       : char.ToUpper(resourceName[0]) + resourceName.Substring(1);

        if (MethodToPermission.TryGetValue(method, out var permissionSuffix))
        {
            var requiredPermission = $"Permissions.{resourceName}.{permissionSuffix}";

            foreach (var role in userRoles)
            {
                var permissionKey = $"role_permissions:{role}";
                var permissionsJson = await cache.GetStringAsync(permissionKey);

                if (!string.IsNullOrEmpty(permissionsJson))
                {
                    var permissions = JsonSerializer.Deserialize<HashSet<string>>(permissionsJson);
                    if (permissions != null && permissions.Contains(requiredPermission))
                    {
                        await next.Invoke();
                        return;
                    }
                }
            }
        }

        var downstreamRoute = context.Items.DownstreamRoute();
        context.Items.SetError(new UnauthorizedError(
                        $"{context.User.Identity?.Name} " +
                        $"unable to access " +
                        $"{downstreamRoute.UpstreamPathTemplate.OriginalValue}"));
    }
}