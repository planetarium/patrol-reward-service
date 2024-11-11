using HotChocolate;
using Libplanet.Common;
using Libplanet.Crypto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class MutationTest
{
    private readonly string _conn;

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

        _conn = connectionString;
    }

    [Fact]
    public async Task PutPlayer_InvalidAddress()
    {
        var avatarAddress = new PrivateKey().ToAddress();
        var agentAddress = new PrivateKey().ToAddress();
        var contextService = Fixtures.GetContextService(_conn, "password");
        var context = await contextService.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var serializedAvatarAddress = avatarAddress.ToString();
        var serializedAgentAddress = agentAddress.ToString();
        var configOptions = new GraphqlClientOptions {Host = "http://heimdall-validator-1.nine-chronicles.com", Port = 80, JwtIssuer = "issuer", JwtSecret = "onsolhjcqbrawkvznmhuukoqunyzyigmwfixgqwvnlqlbpvqfvhfcyslwmqerpyihowcyiksouulydbuuuvlgpfskhzrcrsjorqkwnfxkkosvkkdwcxhjitwyxbfezig"};
        var client = new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(configOptions), new LoggerFactory());
        await Assert.ThrowsAsync<GraphQLException>(() => Mutation.PutAvatar(contextService, client, serializedAvatarAddress, serializedAgentAddress));
        Assert.Empty(context.Avatars);
        await context.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PutPlayer(bool exist)
    {
        var avatarAddress = new Address("0xB9AF8B04890ACD9d69DA584080DB24814eB729B2");
        var agentAddress = new Address("0x60cF1A940f30569d84E591499FddD83B5E37DfBC");
        var player = new AvatarModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow,
            Level = 1
        };

        var contextService = Fixtures.GetContextService(_conn, "password");
        var context = await contextService.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        if (exist)
        {
            context.Avatars.Add(player);
            await context.SaveChangesAsync();
            Assert.Single(context.Avatars);
        }
        else
        {
            Assert.Empty(context.Avatars);
        }
        var serializedAvatarAddress = avatarAddress.ToString();
        var serializedAgentAddress = agentAddress.ToString();
        var configOptions = new GraphqlClientOptions {Host = "http://heimdall-validator-1.nine-chronicles.com", Port = 80, JwtIssuer = "issuer", JwtSecret = "onsolhjcqbrawkvznmhuukoqunyzyigmwfixgqwvnlqlbpvqfvhfcyslwmqerpyihowcyiksouulydbuuuvlgpfskhzrcrsjorqkwnfxkkosvkkdwcxhjitwyxbfezig"};
        var client = new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(configOptions), new LoggerFactory());
        await contextService.PutClaimPolicy(new List<RewardBaseModel>(), true, TimeSpan.FromHours(12), true, 1,
            "password", DateTime.UtcNow, DateTime.MaxValue);
        var result = await Mutation.PutAvatar(contextService, client, serializedAvatarAddress, serializedAgentAddress);
        Assert.NotNull(result);
        Assert.Equal(avatarAddress, result.AvatarAddress);
        Assert.True(result.Level > 0);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task RetryTransaction()
    {
        var contextService = Fixtures.GetContextService(_conn, "password");
        var context = await contextService.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var avatarAddress = new Address("0xaDBa1CF2115c28E1DA761C65a06bADe620D66839");
        var agentAddress = new Address("0x17724085F1d1b5E7a1b7B8857fb7FF14416867C1");
        var avatar = new AvatarModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow,
            ClaimCount = 0,
            LastClaimedAt = DateTime.UtcNow - TimeSpan.FromHours(2),
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

        await context.RewardPolicies.AddAsync(policy);
        await context.Avatars.AddAsync(avatar);
        // await context.SaveChangesAsync();
        var configOptions = new GraphqlClientOptions {Host = "http://heimdall-validator-1.nine-chronicles.com", Port = 80, JwtIssuer = "issuer", JwtSecret = "onsolhjcqbrawkvznmhuukoqunyzyigmwfixgqwvnlqlbpvqfvhfcyslwmqerpyihowcyiksouulydbuuuvlgpfskhzrcrsjorqkwnfxkkosvkkdwcxhjitwyxbfezig"};
        var client = new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(configOptions), new LoggerFactory());
        var privateKey = new PrivateKey();
        var signerOptions = new SignerOptions
        {
            PrivateKey = ByteUtil.Hex(privateKey.ByteArray),
            GenesisHash = "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce"
        };
        var signer = new Signer(new OptionsWrapper<SignerOptions>(signerOptions));
        var txId = await Mutation.ClaimTx(contextService, signer, avatarAddress.ToString(), avatar, policy, new NineChroniclesClient.Avatar());
        Assert.Equal(1, avatar.ClaimCount);
        Assert.Equal(1, context.Transactions.Count(t => t.ClaimCount == 0));
        await Mutation.RetryTransaction(contextService, signer, client, txId, "password");
        Assert.Equal(2, context.Transactions.Count(t => t.ClaimCount == 0));
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task PutClaimPolicy()
    {
        // Arrange
        var contextService = Fixtures.GetContextService(_conn, "password");
        var context = await contextService.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Change these values as necessary to match your requirements
        var rewardModels = new List<RewardBaseModel>(); // You may need to set up some model instances here
        var free = true;
        var interval = TimeSpan.FromHours(12);
        var activate = true;
        var minimumLevel = 1;
        var password = "password";
        var startedAt = DateTime.UtcNow;
        var endedAt = DateTime.MaxValue;

        // Act
        await contextService.PutClaimPolicy(rewardModels, free, interval, activate, minimumLevel, password, startedAt, endedAt);

        // The following assertion needs to be updated according to your own specification
        // Did the call to PutClaimPolicy actually change the state in context in the way you expected?
        // Assert
        var policy = contextService.GetPolicy(true, 1);
        Assert.True(policy.Activate);
        Assert.True(policy.Free);
        Assert.Equal(interval, policy.MinimumRequiredInterval);
        Assert.Equal(startedAt, policy.StartedAt);
        Assert.Equal(endedAt, policy.EndedAt);

        var future = startedAt + TimeSpan.FromHours(1);
        await contextService.PutClaimPolicy(rewardModels, free, interval, activate, minimumLevel, password, future, endedAt);
        Assert.Equal(2, context.RewardPolicies.Count());

        // Cleanup
        await context.Database.EnsureDeletedAsync();
    }
}
