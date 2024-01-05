using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class ContextServiceTest
{
    private readonly string _conn;

    public ContextServiceTest()
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

    [Fact]
    public async Task InsertTransaction()
    {
        var contextService = Fixtures.GetContextService(_conn, "pw");
        var context = await contextService.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var txId = "b7925dbf5a532dd18bba7408177b752ccf6ca2e17034fa18f3e4b8562644fb09";
        var avatarAddress = new Address("0xaDBa1CF2115c28E1DA761C65a06bADe620D66839");
        var agentAddress = new Address("0x17724085F1d1b5E7a1b7B8857fb7FF14416867C1");
        var avatar = new AvatarModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow
        };

        var interval = TimeSpan.FromHours(1);
        List<RewardBaseModel> rewards = new()
        {
            new FungibleItemRewardModel
            {
                ItemId = 800201,
                PerInterval = 20,
                RewardInterval = interval,
            },
            new FungibleAssetValueRewardModel
            {
                Currency = "CRYSTAL",
                PerInterval = 400,
                RewardInterval = interval,
            }
        };
        var policy = new RewardPolicyModel
        {
            Free = true,
            MinimumRequiredInterval = interval,
            Activate = true,
            MinimumLevel = 250,
            Rewards = rewards,
        };

        var claim = new ClaimModel
        {
            Avatar = avatar,
            Policy = policy,
        };
        var transaction = new TransactionModel
        {
            TxId = TxId.FromHex(txId),
            Payload = "payload",
            ClaimCount = 0,
            Claim = claim,
            Avatar = avatar,
            Nonce = 0,
            GasLimit = 4,
            Gas = 1,
        };
        // await contextService.DisposeAsync();
        try
        {
            var avatar2 = new AvatarModel
            {
                AvatarAddress = new PrivateKey().ToAddress(),
                AgentAddress = new PrivateKey().ToAddress(),
                CreatedAt = DateTime.UtcNow
            };
            var claim2 = new ClaimModel
            {
                Avatar = avatar2,
                Policy = policy,
            };
            var duplicatedTransaction = new TransactionModel
            {
                TxId = TxId.FromHex("1b2bd3a1d7af531466e07b189e2353c60832d82100405a222f6a70512ce726dd"),
                Payload = "payload",
                ClaimCount = 0,
                Claim = claim2,
                Avatar = avatar2,
                Nonce = 0,
                GasLimit = 4,
                Gas = 1,
            };
            contextService.InsertTransaction(transaction);
            contextService.InsertTransaction(duplicatedTransaction);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task PutClaimPolicy()
    {
        // Arrange
        var contextService = Fixtures.GetContextService(_conn, "pw");
        var context = await contextService.CreateDbContextAsync();

        var avatarAddress = new Address("0xaDBa1CF2115c28E1DA761C65a06bADe620D66839");
        var agentAddress = new Address("0x17724085F1d1b5E7a1b7B8857fb7FF14416867C1");
        var avatar = new AvatarModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow
        };
        var rewards = new List<RewardBaseModel>
        {
            new FungibleItemRewardModel {ItemId = 800201, PerInterval = 20, RewardInterval = TimeSpan.FromHours(1)},
            new FungibleAssetValueRewardModel {Currency = "CRYSTAL", PerInterval = 400, RewardInterval = TimeSpan.FromHours(1)}
        };
        var policy = new RewardPolicyModel
        {
            Free = true,
            MinimumRequiredInterval = TimeSpan.FromHours(1),
            Activate = true,
            MinimumLevel = 250,
            Rewards = rewards,
        };

        var claim = new ClaimModel
        {
            Avatar = avatar,
            Policy = policy,
        };

        // Clean up before test
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Act
        try
        {
            await context.Claims.AddAsync(claim);
            await context.SaveChangesAsync();

            // Assert
            var retrievedClaim = await context.Claims.FirstOrDefaultAsync(c => c.Avatar.AvatarAddress == avatarAddress);
            Assert.NotNull(retrievedClaim);
            Assert.Equal(policy, retrievedClaim.Policy);
        }
        finally
        {
            // Clean up after test
            await context.Database.EnsureDeletedAsync();
        }
    }
}
