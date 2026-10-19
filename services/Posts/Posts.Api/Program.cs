using Venture.Api.DependencyInjection;
using Venture.Api.Endpoints;
using Venture.Api.Middleware;
using Venture.Application;
using Venture.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
    await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
