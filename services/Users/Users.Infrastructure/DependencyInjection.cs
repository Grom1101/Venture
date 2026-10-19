using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Caching;
using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Abstractions.Notifications;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Application.Abstractions.Transactions;
using Venture.Application.Achievements;
using Venture.Application.Social;
using Venture.Infrastructure.Authentication;
using Venture.Infrastructure.Caching;
using Venture.Infrastructure.Integration;
using Venture.Infrastructure.Messaging;
using Venture.Infrastructure.Persistence;
using Venture.Infrastructure.Persistence.DomainEvents;
using Venture.Infrastructure.Persistence.Repositories;
using Venture.Infrastructure.Persistence.Transactions;
using Venture.Infrastructure.Time;

namespace Venture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<VentureDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.Configure<RedisCacheOptions>(configuration.GetSection(RedisCacheOptions.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services.AddInfrastructureServices(configuration);
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<VentureDbContext>());
        services.AddScoped<IReadDbContext>(provider => provider.GetRequiredService<VentureDbContext>());
        services.AddScoped<IPostCommitActions, PostCommitActions>();

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddSingleton<KafkaProducer>();

        services.AddSocialGrpcClient(configuration);
        services.AddAchievementsGrpcClient(configuration);

        services.AddScoped<IUserAchievementDisplay, GrpcUserAchievementDisplay>();
        services.AddScoped<IFollowStats, GrpcFollowStats>();
        services.AddScoped<INotificationWriter, KafkaNotificationWriter>();

        return services;
    }
}
