using APIGateway.Extensions;
using APIGateway.Handlers;
using APIGateway.Middleware;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddOcelotConfiguration();
builder.Services.AddCorsConfiguration();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication("RefToken")
    .AddScheme<AuthenticationSchemeOptions, ReferenceTokenAuthenticationHandler>("RefToken", options => { });

builder.Services.AddHttpClient();

var app = builder.Build();

var ocelotConfiguration = new OcelotPipelineConfiguration
{
    AuthenticationMiddleware = OcelotAuthenticationMiddleware.Handle
};

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI(options => { options.PathToSwaggerGenerator = "/swagger/docs"; });
    app.UseCors(policy => policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader());
}
else
{
    app.UseCors("AllowFrontend");
}
app.UseAuthentication();
await app.UseOcelot(ocelotConfiguration);

await app.RunAsync();