using APIGateway.Extensions;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddOcelotConfiguration();
builder.Services.AddCorsConfiguration();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI(options => { options.PathToSwaggerGenerator = "/swagger/docs"; });
    //app.UseCors(policy => policy.AllowAnyOrigin()
    //                            .AllowAnyMethod()
    //                            .AllowAnyHeader());
}
else
{
    app.UseCors("AllowFrontend");
}
app.UseHealthChecks("/health");
app.UseAuthentication();

await app.UseOcelot();
await app.RunAsync();