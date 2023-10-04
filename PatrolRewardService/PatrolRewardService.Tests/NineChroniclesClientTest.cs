using Libplanet.Crypto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatrolRewardService.GraphqlTypes;
using Xunit;

namespace PatrolRewardService.Tests;

public class NineChroniclesClientTest
{
    private readonly NineChroniclesClient _client;

    public NineChroniclesClientTest()
    {
        var configOptions = new GraphqlClientOptions {Host = "http://9c-internal-validator-5.nine-chronicles.com", Port = 80};
        _client = new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(configOptions), new LoggerFactory());
    }

    [Fact]
    public async Task GetAvatar()
    {
        var avatarAddress = new Address("0xC86D734Bd2D5857CD25887dB7dBBE252F12087c6");
        var agentAddress = new Address("0x8fF5e1c64860aF7d88b019837a378fBbec75c7D9");
        NineChroniclesClient.Avatar? avatar = await _client.GetAvatar(avatarAddress.ToString());
        Assert.Equal(avatarAddress, avatar.Address);
        Assert.Equal(agentAddress, avatar.AgentAddress);
        Assert.IsType<int>(avatar.Level);
    }

    [Fact]
    public async Task Tip()
    {
        var tip = await _client.Tip();
        Assert.True(tip > 0);
    }
}
