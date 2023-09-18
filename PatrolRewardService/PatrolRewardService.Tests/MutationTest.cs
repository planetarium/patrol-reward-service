using HotChocolate;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class MutationTest
{
    private readonly RewardDbContext _context;

    public MutationTest()
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
            .UseNpgsql(connectionString).Options);
    }

    [Fact]
    public async Task PutPlayer_InvalidAddress()
    {
        var avatarAddress = new PrivateKey().ToAddress();
        var agentAddress = new PrivateKey().ToAddress();
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        var serializedAvatarAddress = avatarAddress.ToString();
        var serializedAgentAddress = agentAddress.ToString();
        var configOptions = new GraphqlClientOptions {Host = "http://9c-internal-validator-5.nine-chronicles.com", Port = 80};
        var client = new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(configOptions), new LoggerFactory());
        await Assert.ThrowsAsync<GraphQLException>(() => Mutation.PutAvatar(_context, client, serializedAvatarAddress, serializedAgentAddress));
        Assert.Empty(_context.Avatars);
        await _context.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PutPlayer(bool exist)
    {
        var avatarAddress = new Address("0xc86d734bd2d5857cd25887db7dbbe252f12087c6");
        var agentAddress = new Address("0x8ff5e1c64860af7d88b019837a378fbbec75c7d9");
        var player = new AvatarModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow,
            Level = 1
        };

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        if (exist)
        {
            _context.Avatars.Add(player);
            await _context.SaveChangesAsync();
            Assert.Single(_context.Avatars);
        }
        else
        {
            Assert.Empty(_context.Avatars);
        }
        var serializedAvatarAddress = avatarAddress.ToString();
        var serializedAgentAddress = agentAddress.ToString();
        var configOptions = new GraphqlClientOptions {Host = "http://9c-internal-validator-5.nine-chronicles.com", Port = 80};
        var client = new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(configOptions), new LoggerFactory());
        var result = await Mutation.PutAvatar(_context, client, serializedAvatarAddress, serializedAgentAddress);
        Assert.NotNull(result);
        Assert.Equal(avatarAddress, result.AvatarAddress);
        Assert.True(result.Level > 0);
        await _context.Database.EnsureDeletedAsync();
    }
}
