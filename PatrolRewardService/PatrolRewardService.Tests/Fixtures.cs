using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;

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

    public static ContextService GetContextService(string connectionString, string password)
    {
#pragma warning disable EF1001
        var contextFactory = new DbContextFactory<RewardDbContext>(null!,
            new DbContextOptionsBuilder<RewardDbContext>().UseNpgsql(connectionString)
                .UseLowerCaseNamingConvention().Options, new DbContextFactorySource<RewardDbContext>());
#pragma warning restore EF1001
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PatrolReward:ApiKey"] = password,
            })
            .Build();
        return new ContextService(contextFactory, config);
    }
}
