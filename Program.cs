using APIGateway.Extensions;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddOcelotConfiguration();
builder.Services.AddJWT(builder.Configuration);

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
app.UseAuthentication();
app.UseAuthorization();
await app.UseOcelot();
await app.RunAsync();