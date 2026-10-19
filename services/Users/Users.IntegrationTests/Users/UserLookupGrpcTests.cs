using System.Net;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Venture.Api.Grpc;
using Venture.Api.Middleware;
using Venture.Application.Users;
using Venture.Application.Users.Queries.GetUserCard;
using Venture.Application.Users.Queries.GetUserCards;
using Venture.Contracts.Users;
using Venture.Domain.Common.Results;

namespace Venture.IntegrationTests.Users;

public class UserLookupGrpcTests
{
    private static readonly Guid ExistingUserId = Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");

    [Fact]
    public async Task Exists_ReturnsFalse_WhenTheUserIsMissing()
    {
        await using var host = await LookupHost.StartAsync(new LookupSender());
        var client = host.Client();

        var reply = await client.ExistsAsync(new UserIdRequest { Id = Guid.NewGuid().ToString("D") });

        reply.Exists.Should().BeFalse();
    }

    [Fact]
    public async Task Exists_ReturnsTrue_WhenTheUserIsFound()
    {
        await using var host = await LookupHost.StartAsync(new LookupSender());
        var client = host.Client();

        var reply = await client.ExistsAsync(new UserIdRequest { Id = ExistingUserId.ToString("D") });

        reply.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetCards_ReturnsTheMappedCard()
    {
        await using var host = await LookupHost.StartAsync(new LookupSender());
        var client = host.Client();

        var reply = await client.GetCardsAsync(new GetCardsRequest { Ids = { ExistingUserId.ToString("D") } });

        reply.Cards.Should().ContainSingle();
        reply.Cards[0].Username.Should().Be("annadoe");
        reply.Cards[0].DisplayName.Should().Be("Anna");
    }

    [Fact]
    public async Task Exists_KeepsTheGrpcStatus_WhenTheHandlerFails()
    {
        await using var host = await LookupHost.StartAsync(new ThrowingSender());
        var client = host.Client();

        var act = () => client.ExistsAsync(new UserIdRequest { Id = ExistingUserId.ToString("D") }).ResponseAsync;

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.Status.Detail.Should().NotContain("Bad gRPC");
        exception.Which.StatusCode.Should().BeOneOf(StatusCode.Internal, StatusCode.Unknown);
    }

    private sealed class LookupHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly GrpcChannel _channel;

        private LookupHost(WebApplication app, GrpcChannel channel)
        {
            _app = app;
            _channel = channel;
        }

        public static async Task<LookupHost> StartAsync(ISender sender)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseSetting("urls", string.Empty);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2);
            });
            builder.Services.AddGrpc();
            builder.Services.AddSingleton(sender);

            var app = builder.Build();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.MapGrpcService<UserLookupService>();
            await app.StartAsync();

            var address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.Single();
            var channel = GrpcChannel.ForAddress(address);
            return new LookupHost(app, channel);
        }

        public UserLookup.UserLookupClient Client() => new(_channel);

        public async ValueTask DisposeAsync()
        {
            await _channel.ShutdownAsync();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed class LookupSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object result = request switch
            {
                GetUserCardQuery card when card.UserId == ExistingUserId =>
                    Result.Success(new UserCardDto(ExistingUserId, "annadoe", "Anna")),
                GetUserCardQuery card =>
                    Result.Failure<UserCardDto>(UserErrors.NotFound(card.UserId)),
                GetUserCardsQuery cards =>
                    Result.Success<IReadOnlyCollection<UserCardDto>>(
                        cards.Ids.Select(id => new UserCardDto(id, "annadoe", "Anna")).ToArray()),
                _ => throw new NotSupportedException(request.GetType().Name)
            };

            return Task.FromResult((TResponse)result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("база недоступна");

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
