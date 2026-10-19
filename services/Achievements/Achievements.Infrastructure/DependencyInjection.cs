using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Application.Abstractions.Transactions;
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

        return services.AddInfrastructureServices(configuration);
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<VentureDbContext>());
        services.AddScoped<IReadDbContext>(provider => provider.GetRequiredService<VentureDbContext>());
        services.AddScoped<IPostCommitActions, PostCommitActions>();

        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IAchievementProgressStore, AchievementProgressStore>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddHostedService<AchievementFactsConsumer>();

        return services;
    }
}
