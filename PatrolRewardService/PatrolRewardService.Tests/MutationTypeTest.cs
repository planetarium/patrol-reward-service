using HotChocolate;
using HotChocolate.Execution;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatrolRewardService.Models;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace PatrolRewardService.Tests;

public class MutationTypeTest
{
    private readonly ServiceProvider _provider;
    private readonly RequestExecutorProxy _executor;
    private readonly IDbContextFactory<RewardDbContext> _contextFactory;

    public MutationTypeTest()
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
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PutRewardPolicy(bool exist)
    {
        var context = await _contextFactory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var query = @"mutation {
  putRewardPolicy(
    activate: true
    free: true
    interval: ""PT6H""
        minimumLevel: 50
        password: ""password""
        rewards: [
        { currency: ""CRYSTAL"", perInterval: 50, rewardInterval: ""PT6H"" }
        {
            itemId: 40000
            fungibleId: ""1"",
            perInterval: 50
            rewardInterval: ""PT6H""
        }
        {
            itemId: 50000
            fungibleId: ""2"",
            perInterval: 1
            rewardInterval: ""PT6H""
        }
        ]
        )
    }
    ";
        var request = QueryRequestBuilder.New()
            .SetQuery(query)
            .SetServices(_provider)
            .Create();
        if (exist)
        {
            await _executor.ExecuteAsync(request);
        }

        IExecutionResult result = await _executor.ExecuteAsync(request);
        var data = ExecuteRequestAsStreamAsync(result);
        data.MatchSnapshot(SnapshotNameExtension.Create(exist));
        Assert.Single(context.RewardPolicies);
        Assert.Equal(3, context.Rewards.Count());
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task PutRewardPolicy_Throw_UnauthorizedAccessException()
    {
        var context = await _contextFactory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var query = @"mutation {
  putRewardPolicy(
    activate: true
    free: true
    interval: ""PT6H""
        minimumLevel: 50
        password: ""invalid""
        rewards: [
        { currency: ""CRYSTAL"", perInterval: 50, rewardInterval: ""PT6H"" }
        {
            itemId: 40000
            fungibleId: ""1"",
            perInterval: 50
            rewardInterval: ""PT6H""
        }
        {
            itemId: 50000
            fungibleId: ""2"",
            perInterval: 1
            rewardInterval: ""PT6H""
        }
        ]
        )
    }
    ";
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
