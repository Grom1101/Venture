using Venture.Api.Endpoints;

namespace Venture.Api.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddEndpoints(typeof(ApiServiceCollectionExtensions).Assembly);
        services.AddOpenApi();

        return services;
    }
}
