using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
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
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
        });

        return services;
    }

    public static IServiceCollection AddJWT(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = configuration["JWT:Secret"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT Secret Key is missing in configuration.");
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer("Bearer", o =>
        {
            o.RequireHttpsMetadata = false;
            o.SaveToken = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                //ValidIssuer = "https://localhost:5001/",
                //ValidAudience = "388D45FA-B36B-4988-BA59-B187D329C207",
                IssuerSigningKey = key
            };
        });
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddRedis(this IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost";
            options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
            {
                AbortOnConnectFail = true,
                EndPoints = { options.Configuration }
            };
        });

        return services;
    }
}