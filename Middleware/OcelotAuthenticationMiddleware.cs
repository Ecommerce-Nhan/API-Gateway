using Ocelot.Configuration;
using Ocelot.Middleware;

namespace APIGateway.Middleware;

public class OcelotAuthenticationMiddleware
{
    private static readonly ILogger<OcelotAuthenticationMiddleware> _logger;
    static OcelotAuthenticationMiddleware()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<OcelotAuthenticationMiddleware>();
    }
    public static async Task Handle(HttpContext context, Func<Task> next)
    {
        var downstreamRoute = context.Items.DownstreamRoute();
        if (IsOptionsHttpMethod(context) || IsAuthMethod(context, downstreamRoute))
        {
            await next.Invoke();
            return;
        }

        await next.Invoke();
    }

    private static bool IsOptionsHttpMethod(HttpContext httpContext)
    {
        return httpContext.Request.Method.ToUpper() == "OPTIONS";
    }

    private static bool IsAuthMethod(HttpContext httpContext, DownstreamRoute downstreamRoute)
    {
        return httpContext.Request.Path.Value?.ToUpper() == "/API/IDENTITY/TOKEN";
    }
}
