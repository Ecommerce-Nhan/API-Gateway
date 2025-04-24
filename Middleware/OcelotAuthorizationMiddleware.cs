using Azure.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Ocelot.Authorization;
using Ocelot.Configuration;
using Ocelot.Middleware;
using System.Security.Claims;
using System.Text.Json;

namespace APIGateway.Middleware;

public class OcelotAuthorizationMiddleware
{
    private static readonly ILogger<OcelotAuthorizationMiddleware> _logger;
    static OcelotAuthorizationMiddleware()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<OcelotAuthorizationMiddleware>();
    }
    public static async Task Handle(HttpContext context, Func<Task> next)
    {
        var downstreamRoute = context.Items.DownstreamRoute();
        ClaimsPrincipal claimsPrincipal = context.User;

        if (IsOptionsHttpMethod(context) || IsAuthMethod(context, downstreamRoute))
        {
            await next.Invoke();
            return;
        }

        if (claimsPrincipal.Identity == null)
        {
            context.Items.SetError(new UnauthenticatedError("Unauthorized"));
            return;
        }


        if (!await Authorize(context, downstreamRoute))
        {
            context.Items.SetError(new UnauthorizedError("Forbidden"));
            return;
        }
        await next.Invoke();
    }
    private static async Task<bool> Authorize(HttpContext context, DownstreamRoute downstreamRoute)
    {
        var cache = context.RequestServices.GetRequiredService<IDistributedCache>();
        var claimsPrincipal = context.User;
        var userRoles = claimsPrincipal.Claims.Where(c => c.Type == ClaimTypes.Role)
                                              .Select(c => c.Value)
                                              .ToList();

        if (!userRoles.Any())
        {
            return false;
        }

        var method = context.Request.Method.ToUpper();
        var path = context.Request.Path.Value?.Trim('/') ?? string.Empty;
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resourceName = pathSegments[2];
        resourceName = string.IsNullOrEmpty(resourceName)
                       ? "Unknown"
                       : char.ToUpper(resourceName[0]) + resourceName.Substring(1);
        _logger.LogInformation("Resource Access Attempt: {ResourceName}, Method: {Method}, Roles: {Roles}",
           resourceName, method, string.Join(", ", userRoles));

        if (MethodToPermission.TryGetValue(method, out var permissionSuffix))
        {
            var requiredPermission = $"Permissions.{resourceName}.{permissionSuffix}";
            _logger.LogInformation(requiredPermission);

            foreach (var role in userRoles)
            {
                var permissionKey = $"role_permissions:{role}";
                var permissionsJson = await cache.GetStringAsync(permissionKey);

                if (!string.IsNullOrEmpty(permissionsJson))
                {
                    var permissions = JsonSerializer.Deserialize<HashSet<string>>(permissionsJson);
                    if (permissions != null && permissions.Contains(requiredPermission))
                    {
                        return true;
                    }
                }
            }
        }

        context.Items.SetError(new UnauthorizedError($"{context.User.Identity?.Name} unable to access" +
                                                     $" {downstreamRoute.UpstreamPathTemplate.OriginalValue}"));
        return false;
    }
    private static readonly Dictionary<string, string> MethodToPermission = new()
    {
        { "GET", "View" },
        { "POST", "Create" },
        { "PUT", "Edit" },
        { "DELETE", "Delete" }
    };
    private static bool IsOptionsHttpMethod(HttpContext httpContext)
    {
        return httpContext.Request.Method.ToUpper() == "OPTIONS";
    }
    private static bool IsAuthMethod(HttpContext httpContext, DownstreamRoute downstreamRoute)
    {
        return httpContext.Request.Path.Value?.ToUpper() == "/API/IDENTITY/TOKEN";
    }
}