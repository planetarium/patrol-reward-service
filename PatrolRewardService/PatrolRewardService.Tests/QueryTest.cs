using System.Security.Cryptography;
using Bencodex;
using Lib9c;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Nekoyume.Action.Garages;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class QueryTest
{
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

        var contextService = Fixtures.GetContextService(_conn);
        var context = contextService.DbContext;
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        context.Avatars.Add(player);
        await context.SaveChangesAsync();
        Assert.Single(context.Avatars);
        var serializedAvatarAddress = hex ? avatarAddress.ToHex() : avatarAddress.ToString();
        var serializedAgentAddress = hex ? agentAddress.ToHex() : agentAddress.ToString();
        var query = new Query();
        var result = query.GetAvatar(contextService, serializedAvatarAddress, serializedAgentAddress);
        Assert.NotNull(result);
        Assert.Equal(avatarAddress, result.AvatarAddress);
        await context.Database.EnsureDeletedAsync();
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
        var contextService = Fixtures.GetContextService(_conn);
        var context = contextService.DbContext;
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
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
            await context.RewardPolicies.AddAsync(policy);
        }

        await context.SaveChangesAsync();

        var result = Query.GetPolicy(contextService, true, level);
        Assert.Equal(expectedLevel, result.MinimumLevel);
    }
}
