using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace APIGateway.Extensions;

public static class ServiceCollections
{
    public static IServiceCollection AddJWT(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer("Bearer", o =>
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("IxrAjDoa2FqElO7IhrSrUJELhUckePEPVpaePlS_Xaw"));
            o.RequireHttpsMetadata = false;
            o.SaveToken = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                RoleClaimType = "role",
                NameClaimType = "name",
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidIssuer = "https://localhost:5001/",
                ValidAudience = "a6c18d2e-4413-436a-93d3-cc4363f3944d",
                IssuerSigningKey = key
            };
        });
        services.AddAuthorization();

        return services;
    }
}
