using FluentAssertions;
using Grpc.Core;
using Venture.Contracts.Users;
using Social = Venture.Contracts.Social;
using Venture.Domain.Users.ValueObjects;
using Venture.Infrastructure.Integration;
using UserCard = Venture.Application.Integration.UserCard;
using UserCardMessage = Venture.Contracts.Users.UserCard;

namespace Venture.Application.Tests.Integration;

public class GrpcClientTests
{
    private static readonly Guid AuthorId = Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");

    [Fact]
    public async Task Exists_ReturnsTheServerFlag()
    {
        var client = new ScriptedUsers { UserExists = true };
        var repository = new GrpcUserRepository(client);

        var exists = await repository.ExistsAsync(UserId.From(AuthorId).Value, CancellationToken.None);

        exists.Should().BeTrue();
        client.LastUserId.Should().Be(AuthorId.ToString("D"));
    }

    [Fact]
    public async Task GetCards_SkipsTheCall_WhenThereAreNoIds()
    {
        var client = new ScriptedUsers();
        var cards = new GrpcUserCards(client);

        var result = await cards.GetAsync([], CancellationToken.None);

        result.Should().BeEmpty();
        client.CardRequests.Should().Be(0);
    }

    [Fact]
    public async Task GetCards_MapsTheReply()
    {
        var client = new ScriptedUsers
        {
            Cards =
            {
                new UserCardMessage
                {
                    Id = AuthorId.ToString("D"),
                    Username = "annadoe",
                    DisplayName = "Anna"
                }
            }
        };

        var result = await new GrpcUserCards(client).GetAsync([AuthorId], CancellationToken.None);

        result.Should().ContainKey(AuthorId);
        result[AuthorId].Should().Be(new UserCard(AuthorId, "annadoe", "Anna"));
    }

    [Fact]
    public async Task GetFollowing_MapsIds()
    {
        var followedId = Guid.Parse("2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11");
        var client = new ScriptedSocial();
        client.Following.Add(followedId.ToString("D"));

        var result = await new GrpcFollowedUsers(client).GetFollowingIdsAsync(AuthorId, CancellationToken.None);

        result.Should().Equal(followedId);
    }

    private sealed class ScriptedUsers : UserLookup.UserLookupClient
    {
        public bool UserExists { get; set; }

        public string? LastUserId { get; private set; }

        public int CardRequests { get; private set; }

        public List<UserCardMessage> Cards { get; } = [];

        public override AsyncUnaryCall<ExistsReply> ExistsAsync(UserIdRequest request, CallOptions options)
        {
            LastUserId = request.Id;
            return Unary(new ExistsReply { Exists = UserExists });
        }

        public override AsyncUnaryCall<GetCardsReply> GetCardsAsync(GetCardsRequest request, CallOptions options)
        {
            CardRequests++;
            var reply = new GetCardsReply();
            reply.Cards.AddRange(Cards);
            return Unary(reply);
        }
    }

    private sealed class ScriptedSocial : Social.SocialLookup.SocialLookupClient
    {
        public List<string> Following { get; } = [];

        public override AsyncUnaryCall<Social.FollowingReply> GetFollowingAsync(
            Social.UserIdRequest request,
            CallOptions options)
        {
            var reply = new Social.FollowingReply();
            reply.Ids.AddRange(Following);
            return Unary(reply);
        }
    }

    private static AsyncUnaryCall<T> Unary<T>(T response) =>
        new(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
}
