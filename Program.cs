using APIGateway.Extensions;
using APIGateway.Middleware;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddOcelotConfiguration();
builder.Services.AddCorsConfiguration();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowFrontend");

var ocelotConfiguration = new OcelotPipelineConfiguration
{
    AuthenticationMiddleware = OcelotAuthenticationMiddleware.Handle
};
await app.UseOcelot(ocelotConfiguration);


if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI(options => { options.PathToSwaggerGenerator = "/swagger/docs"; });
}


await app.RunAsync();