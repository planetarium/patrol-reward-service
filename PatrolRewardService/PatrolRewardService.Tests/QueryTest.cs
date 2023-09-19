using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class QueryTest
{
    private readonly RewardDbContext _context;

    public QueryTest()
    {
        var host = Environment.GetEnvironmentVariable("TEST_DB_HOST") ?? "localhost";
        var userName = Environment.GetEnvironmentVariable("TEST_DB_USER") ?? "postgres";
        var pw = Environment.GetEnvironmentVariable("TEST_DB_PW");
        var connectionString = $"Host={host};Username={userName};Database={GetType().Name};";
        if (!string.IsNullOrEmpty(pw))
        {
            connectionString += $"Password={pw};";
        }

        _context = new RewardDbContext(new DbContextOptionsBuilder<RewardDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging()
            .Options);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPlayer(bool hex)
    {
        var avatarAddress = new PrivateKey().ToAddress();
        var agentAddress = new PrivateKey().ToAddress();
        var player = new AvatarModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        _context.Avatars.Add(player);
        await _context.SaveChangesAsync();
        Assert.Single(_context.Avatars);
        var serializedAvatarAddress = hex ? avatarAddress.ToHex() : avatarAddress.ToString();
        var serializedAgentAddress = hex ? agentAddress.ToHex() : agentAddress.ToString();
        var result = Query.GetAvatar(_context, serializedAvatarAddress, serializedAgentAddress);
        Assert.NotNull(result);
        Assert.Equal(avatarAddress, result.AvatarAddress);
        await _context.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(50, 50)]
    [InlineData(149, 50)]
    [InlineData(150, 150)]
    [InlineData(250, 250)]
    [InlineData(350, 250)]
    public async Task GetPolicy(int level, int expectedLevel)
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        foreach (var (minimumLevel, maxLevel) in new (int, int?)[]
                 {
                     (50, 149),
                     (150, 249),
                     (250, null)
                 })
        {
            var policy = new RewardPolicyModel
            {
                Free = true,
                MinimumRequiredInterval = TimeSpan.FromSeconds(0),
                Activate = true,
                MinimumLevel = minimumLevel,
                MaxLevel = maxLevel,
            };
            await _context.RewardPolicies.AddAsync(policy);
        }

        await _context.SaveChangesAsync();

        var result = Query.GetPolicy(_context, true, level);
        Assert.Equal(expectedLevel, result.MinimumLevel);
    }
}
