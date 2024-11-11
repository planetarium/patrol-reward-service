using System.Net.Http.Headers;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
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
        var graphqlHost = Environment.GetEnvironmentVariable("TEST_GRAPHQL_HOST")!;
        var jwtSecret = Environment.GetEnvironmentVariable("TEST_JWT_SECRET")!;
        var configOptions = new GraphqlClientOptions {Host = graphqlHost, Port = 80, JwtIssuer = "issuer", JwtSecret = jwtSecret};
        _client = new NineChroniclesClient(new OptionsWrapper<GraphqlClientOptions>(configOptions), new LoggerFactory());
    }

    [Fact]
    public async Task GetAvatar()
    {
        var avatarAddress = new Address("0xB9AF8B04890ACD9d69DA584080DB24814eB729B2");
        var agentAddress = new Address("0x60cF1A940f30569d84E591499FddD83B5E37DfBC");
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

    [Fact]
    public void RequestHeader()
    {
        var request = new NineChroniclesClient.GraphQLHttpRequestWithAuth
        {
            Query = "",
            Authentication = new AuthenticationHeaderValue("Bearer","test"),
        };
        var msg = request.ToHttpRequestMessage(new GraphQLHttpClientOptions(), new NewtonsoftJsonSerializer());
        Assert.NotNull(msg.Headers.Authorization);
        Assert.Equal(request.Authentication, msg.Headers.Authorization);
    }

    [Fact]
    public async Task Nonce()
    {
        var nonce = await _client.Nonce();
        Assert.True(nonce > 0L);
    }
}
