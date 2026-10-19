using System.Reflection;

namespace Venture.Api.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.DefinedTypes
                     .Where(type => type is { IsAbstract: false, IsInterface: false }
                                    && type.IsAssignableTo(typeof(IEndpoint))))
        {
            services.Add(ServiceDescriptor.Transient(typeof(IEndpoint), type));
        }

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
            endpoint.MapEndpoint(app);

        return app;
    }
}
