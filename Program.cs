using APIGateway.Extensions;
using APIGateway.Middleware;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddOcelotConfiguration();
builder.Services.AddCorsConfiguration();
builder.Services.AddSwaggerGen();

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
await app.UseOcelot(ocelotConfiguration);

await app.RunAsync();