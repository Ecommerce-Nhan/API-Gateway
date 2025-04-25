using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using System.Text;

namespace APIGateway.Extensions;

public static class ServiceCollections
{
    public static WebApplicationBuilder AddOcelotConfiguration(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        var routes = Path.Combine("Routes", environment);

        builder.Configuration.AddOcelotWithSwaggerSupport(options =>
        {
            options.Folder = routes;
        });
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                             .AddOcelot(routes, builder.Environment)
                             .AddJsonFile("ocelot.json", optional: true, reloadOnChange: true)
                             .AddEnvironmentVariables();

        builder.Services.AddOcelot(builder.Configuration);
        builder.Services.AddSwaggerForOcelot(builder.Configuration);

        return builder;
    }

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
                policy =>
                {
                    policy.WithOrigins("https://localhost:7139")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
        });

        return services;
    }

}