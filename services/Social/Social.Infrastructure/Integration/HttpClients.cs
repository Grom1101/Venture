using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Integration;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Integration;

internal static class ServiceClientRegistration
{
    public static IHttpClientBuilder AddServiceClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string name)
    {
        var address = configuration[$"Services:{name}"]
            ?? throw new InvalidOperationException($"Services:{name} is not configured.");

        if (!address.EndsWith('/'))
            address += "/";

        return services.AddHttpClient(name, client => client.BaseAddress = new Uri(address));
    }
}

internal sealed class HttpUserRepository(IHttpClientFactory httpClientFactory) : IUserRepository
{
    public async Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Users");
        var response = await client.GetAsync($"internal/users/{id.Value:D}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }
}

internal sealed class HttpUserProfiles(IHttpClientFactory httpClientFactory) : IUserProfiles
{
    public async Task<UserProfileRef?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("Users");
        var response = await client.GetAsync($"internal/users/{userId:D}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileRef>(cancellationToken);
    }
}
