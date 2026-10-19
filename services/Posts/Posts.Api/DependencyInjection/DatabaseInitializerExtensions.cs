using Microsoft.EntityFrameworkCore;
using Venture.Infrastructure.Persistence;

namespace Venture.Api.DependencyInjection;

public static class DatabaseInitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VentureDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
