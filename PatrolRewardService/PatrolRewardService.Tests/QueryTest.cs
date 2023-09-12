using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class QueryTest
{
    private readonly ServiceContext _context;

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

        _context = new ServiceContext(new DbContextOptionsBuilder<ServiceContext>()
            .UseNpgsql(connectionString).Options);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPlayer(bool hex)
    {
        var avatarAddress = new PrivateKey().ToAddress();
        var agentAddress = new PrivateKey().ToAddress();
        var player = new PlayerModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        Assert.Single(_context.Players);

        var query = new Query();
        var address = hex ? avatarAddress.ToHex() : avatarAddress.ToString();
        var result = query.GetPlayer(_context, address);
        Assert.NotNull(result);
        Assert.Equal(avatarAddress, result.AvatarAddress);
        await _context.Database.EnsureDeletedAsync();
    }
}
