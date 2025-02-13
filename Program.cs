using APIGateway.Extensions;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

var routes = "Routes";
builder.Configuration.AddOcelotWithSwaggerSupport(options =>
{
    options.Folder = routes;
});
builder.Services.AddJWT(builder.Configuration);
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddOcelot(routes, builder.Environment)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.AddJsonFile("ocelot.json");

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI(options => { options.PathToSwaggerGenerator = "/swagger/docs"; });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
await app.UseOcelot();
await app.RunAsync();