using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class QueryTest
{
    private readonly RewardDbContext _context;
    private readonly string _conn;

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

        _conn = connectionString;
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
}
