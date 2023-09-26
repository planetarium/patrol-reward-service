using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace PatrolRewardService.Tests;

public static class Fixtures
{
    public static RewardDbContext GetDbContext(string connectionString)
    {
        return new RewardDbContext(new DbContextOptionsBuilder<RewardDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging()
            .Options);
    }

    public static ContextService GetContextService(string connectionString)
    {
#pragma warning disable EF1001
        var contextFactory = new DbContextFactory<RewardDbContext>(null!,
            new DbContextOptionsBuilder<RewardDbContext>().UseNpgsql(connectionString)
                .UseLowerCaseNamingConvention().Options, new DbContextFactorySource<RewardDbContext>());
#pragma warning restore EF1001
        return new ContextService(contextFactory);
    }
}
