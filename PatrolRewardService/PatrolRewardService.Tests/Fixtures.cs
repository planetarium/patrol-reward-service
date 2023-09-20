using Microsoft.EntityFrameworkCore;

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
}
