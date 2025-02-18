using APIGateway.Extensions;
using APIGateway.Middleware;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddOcelotConfiguration();
builder.Services.AddJWT(builder.Configuration);
builder.Services.AddRedis();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI(options => { options.PathToSwaggerGenerator = "/swagger/docs"; });
}
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
var configuration = new OcelotPipelineConfiguration
{
    AuthorizationMiddleware = OcelotAuthorizationMiddleware.Handle
};
app.UseAuthentication();
app.UseAuthorization();
await app.UseOcelot(configuration);
await app.RunAsync();