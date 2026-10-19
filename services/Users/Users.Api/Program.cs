using Microsoft.AspNetCore.Server.Kestrel.Core;
using Venture.Api.DependencyInjection;
using Venture.Api.Endpoints;
using Venture.Api.Grpc;
using Venture.Api.Middleware;
using Venture.Application;
using Venture.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Без TLS HTTP/2 не договаривается с HTTP/1.1 на одном порту, поэтому gRPC слушает отдельно.
builder.WebHost.ConfigureKestrel((context, server) =>
{
    server.ListenLocalhost(EndpointPort(context.Configuration, "Http"), listen =>
        listen.Protocols = HttpProtocols.Http1);
    server.ListenLocalhost(EndpointPort(context.Configuration, "Grpc"), listen =>
        listen.Protocols = HttpProtocols.Http2);
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

builder.Services.AddGrpc();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<UserLookupService>();
app.MapEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
    await app.InitializeDatabaseAsync();

app.Run();

static int EndpointPort(IConfiguration configuration, string name)
{
    var address = configuration[$"Endpoints:{name}"]
        ?? throw new InvalidOperationException($"Endpoints:{name} is not configured.");

    return new Uri(address).Port;
}

public partial class Program;
