using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Integration;
using Venture.Domain.Users.ValueObjects;
using Social = Venture.Contracts.Social;
using Users = Venture.Contracts.Users;
using UserCard = Venture.Application.Integration.UserCard;

namespace Venture.Infrastructure.Integration;

internal static class GrpcClientRegistration
{
    public static IServiceCollection AddUsersGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGrpcClient<Users.UserLookup.UserLookupClient>(options =>
            options.Address = ServiceAddress(configuration, "Users"));

        return services;
    }

    public static IServiceCollection AddSocialGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGrpcClient<Social.SocialLookup.SocialLookupClient>(options =>
            options.Address = ServiceAddress(configuration, "Social"));

        return services;
    }

    private static Uri ServiceAddress(IConfiguration configuration, string name)
    {
        var address = configuration[$"Grpc:{name}"]
            ?? throw new InvalidOperationException($"Grpc:{name} is not configured.");

        return new Uri(address);
    }
}

internal sealed class GrpcUserRepository(Users.UserLookup.UserLookupClient client) : IUserRepository
{
    public async Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken)
    {
        var reply = await client.ExistsAsync(
            new Users.UserIdRequest { Id = id.Value.ToString("D") },
            cancellationToken: cancellationToken);

        return reply.Exists;
    }
}

internal sealed class GrpcFollowedUsers(Social.SocialLookup.SocialLookupClient client) : IFollowedUsers
{
    public async Task<IReadOnlyCollection<Guid>> GetFollowingIdsAsync(
        Guid followerId,
        CancellationToken cancellationToken)
    {
        var reply = await client.GetFollowingAsync(
            new Social.UserIdRequest { Id = followerId.ToString("D") },
            cancellationToken: cancellationToken);

        return reply.Ids.Select(ReadId).ToArray();
    }

    private static Guid ReadId(string value)
    {
        if (Guid.TryParse(value, out var id))
            return id;

        throw new InvalidOperationException("В списке подписок нет корректного идентификатора.");
    }
}

internal sealed class GrpcUserCards(Users.UserLookup.UserLookupClient client) : IUserCards
{
    public async Task<IReadOnlyDictionary<Guid, UserCard>> GetAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new Dictionary<Guid, UserCard>();

        var request = new Users.GetCardsRequest();
        request.Ids.AddRange(ids.Select(id => id.ToString("D")));

        var reply = await client.GetCardsAsync(request, cancellationToken: cancellationToken);
        var cards = new Dictionary<Guid, UserCard>(reply.Cards.Count);

        foreach (var card in reply.Cards)
        {
            var id = ReadId(card.Id);
            cards[id] = new UserCard(id, card.Username, card.DisplayName);
        }

        return cards;
    }

    private static Guid ReadId(string value)
    {
        if (Guid.TryParse(value, out var id))
            return id;

        throw new InvalidOperationException("В карточке пользователя нет корректного идентификатора.");
    }
}
