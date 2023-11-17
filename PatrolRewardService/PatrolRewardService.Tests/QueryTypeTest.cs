using HotChocolate;
using HotChocolate.Execution;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatrolRewardService.Models;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace PatrolRewardService.Tests;

public class QueryTypeTest
{
    private readonly ServiceProvider _provider;
    private readonly RequestExecutorProxy _executor;
    private readonly IDbContextFactory<RewardDbContext> _contextFactory;
    private readonly RewardPolicyModel _policy;

    public QueryTypeTest()
    {
        var host = Environment.GetEnvironmentVariable("TEST_DB_HOST") ?? "localhost";
        var userName = Environment.GetEnvironmentVariable("TEST_DB_USER") ?? "postgres";
        var pw = Environment.GetEnvironmentVariable("TEST_DB_PW");
        var connectionString = $"Host={host};Username={userName};Database={GetType().Name};";
        if (!string.IsNullOrEmpty(pw))
        {
            connectionString += $"Password={pw};";
        }
        var privateKey = new PrivateKey();
        var blockHash = new BlockHash();
        var myConfiguration = new Dictionary<string, string>
        {
            {"ConnectionStrings:PatrolReward", connectionString},
            {"GraphqlClientConfig:Host", "http://9c-internal-validator-5.nine-chronicles.com"},
            {"GraphqlClientConfig:Port", "80"},
            {"SignerConfig:PrivateKey", ByteUtil.Hex(privateKey.ByteArray)},
            {"SignerConfig:GenesisHash", ByteUtil.Hex(blockHash.ByteArray)},
            {"PatrolReward:ApiKey", "password"},
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();
        var startUp = new StartUp(configuration);
        IServiceCollection services = new ServiceCollection();
        startUp.ConfigureServices(services);
        _provider = services.BuildServiceProvider();
        var resolver = _provider.GetRequiredService<IRequestExecutorResolver>();
        _executor = new RequestExecutorProxy(resolver, Schema.DefaultName);
        _contextFactory = _provider.GetRequiredService<IDbContextFactory<RewardDbContext>>();
        var interval = TimeSpan.FromHours(4);
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
        _policy = new RewardPolicyModel
        {
            Free = true,
            MinimumRequiredInterval = interval,
            Activate = true,
            MinimumLevel = 250,
            Rewards = rewards,
        };
    }

    [Fact]
    public async Task GetAvatar()
    {
        var context = await _contextFactory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var avatarAddress = new Address("0xaDBa1CF2115c28E1DA761C65a06bADe620D66839");
        var agentAddress = new Address("0x17724085F1d1b5E7a1b7B8857fb7FF14416867C1");
        var player = new AvatarModel
        {
            AvatarAddress = avatarAddress,
            AgentAddress = agentAddress,
            CreatedAt = DateTime.UtcNow
        };
        context.Avatars.Add(player);
        await context.SaveChangesAsync();
        Assert.Single(context.Avatars);
        var query = @"query($avatarAddress: address!, $agentAddress: address!) {
  avatar(
    avatarAddress: $avatarAddress
        agentAddress: $agentAddress
            ) {
            avatarAddress
                agentAddress
            level
        }
    }
    ";
        var request = QueryRequestBuilder.New()
            .SetQuery(query)
            .AddVariableValue("avatarAddress", avatarAddress.ToString())
            .AddVariableValue("agentAddress", agentAddress.ToString())
            .SetServices(_provider)
            .Create();
        IExecutionResult result = await _executor.ExecuteAsync(request);
        var data = ExecuteRequestAsStreamAsync(result);
        data.MatchSnapshot();
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task GetPolicy()
    {
        var context = await _contextFactory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await context.RewardPolicies.AddAsync(_policy);
        await context.SaveChangesAsync();

        var query = @"query {
  policy(level: 81, free: true) {
    activate
    minimumLevel
    maxLevel
    minimumRequiredInterval
      rewards {
    ... on FungibleAssetValueRewardModel {
      currency
      perInterval
      rewardInterval
    }
    ... on FungibleItemRewardModel {
      itemId
      perInterval
      rewardInterval
    }
  }
  }
}";

        var request = QueryRequestBuilder.New()
            .SetQuery(query)
            .SetServices(_provider)
            .Create();
        IExecutionResult result = await _executor.ExecuteAsync(request);
        var data = ExecuteRequestAsStreamAsync(result);
        data.MatchSnapshot();
    }

    [Fact]
    public async Task Transaction()
    {
        var context = await _contextFactory.CreateDbContextAsync();
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

        var claim = new ClaimModel
        {
            Avatar = avatar,
            Policy = _policy,
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
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        var query = $@"query {{
  transaction(txId: ""{txId}"") {{
        txId
        payload
        avatarAddress
        nonce
        result
    }}
}}";

        var request = QueryRequestBuilder.New()
            .SetQuery(query)
            .SetServices(_provider)
            .Create();
        IExecutionResult result = await _executor.ExecuteAsync(request);
        var data = ExecuteRequestAsStreamAsync(result);
        data.MatchSnapshot();
        await context.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData(TransactionStatus.CREATED)]
    [InlineData(TransactionStatus.STAGING)]
    public async Task Transactions(TransactionStatus status)
    {
        var context = await _contextFactory.CreateDbContextAsync();
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

        var claim = new ClaimModel
        {
            Avatar = avatar,
            Policy = _policy,
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
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        var query = @$"query {{
  transactions(where: {{
    result: {{
      eq: {status}
    }}
  }}) {{
    txId
    payload
    avatarAddress
    nonce
    result
  }}
}}";

        var request = QueryRequestBuilder.New()
            .SetQuery(query)
            .SetServices(_provider)
            .Create();
        IExecutionResult result = await _executor.ExecuteAsync(request);
        var data = ExecuteRequestAsStreamAsync(result);
        data.MatchSnapshot(SnapshotNameExtension.Create(status));
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task InvalidTxCount()
    {
        var context = await _contextFactory.CreateDbContextAsync();
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

        var claim = new ClaimModel
        {
            Avatar = avatar,
            Policy = _policy,
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
            Result = TransactionStatus.INVALID,
        };
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        var query = "query { invalidTxCount() }";

        var request = QueryRequestBuilder.New()
            .SetQuery(query)
            .SetServices(_provider)
            .Create();
        IExecutionResult result = await _executor.ExecuteAsync(request);
        var data = ExecuteRequestAsStreamAsync(result);
        data.MatchSnapshot();
        await context.Database.EnsureDeletedAsync();
        
    }

    private static string ExecuteRequestAsStreamAsync(
        IExecutionResult result)
    {
        result.ExpectQueryResult();
        return result.ToJson();
    }
}
