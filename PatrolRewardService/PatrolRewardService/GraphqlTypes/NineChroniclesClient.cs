using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class NineChroniclesClient
{
    private readonly GraphQLHttpClient _client;
    private readonly ILogger<NineChroniclesClient> _logger;
    private readonly SigningCredentials _cred;
    private readonly string _issuer;

    public NineChroniclesClient(IOptions<GraphqlClientOptions> options, ILoggerFactory loggerFactory)
    {
        var graphqlClientOptions = options.Value;
        var clientOptions = new GraphQLHttpClientOptions
        {
            EndPoint = new Uri($"{graphqlClientOptions.Host}:{graphqlClientOptions.Port}/graphql")
        };
        _client = new GraphQLHttpClient(clientOptions, new NewtonsoftJsonSerializer());
        _logger = loggerFactory.CreateLogger<NineChroniclesClient>();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(graphqlClientOptions.JwtSecret));
        _cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _issuer = graphqlClientOptions.JwtIssuer;
    }

    public async Task<Avatar> GetAvatar(string avatarAddress)
    {
        var query = @"
query($avatarAddress: Address!) {
    stateQuery {
        avatar(avatarAddress: $avatarAddress) {
            address
            agentAddress
            level
        }
    }
}";
        var request = new GraphQLHttpRequestWithAuth
        {
            Query = query,
            Variables = new
            {
                avatarAddress
            },
            Authentication = new AuthenticationHeaderValue("Bearer",Token())
        };

        GraphQLResponse<GetAvatarResult> resp;
        try
        {
            resp = await _client.SendQueryAsync<GetAvatarResult>(request);
        }
        catch (Exception e)
        {
            _logger.LogError("{Msg}", e.Message);
            throw;
        }

        if (resp.Errors is null) return resp.Data.StateQuery.Avatar;

        var msg = resp.Errors.Aggregate("", (current, error) => current + error.Message + "\n");
        throw new GraphQLException(msg);
    }

    public async Task<string> StageTx(Transaction tx)
    {
        var query = "mutation($payload: String!) { stageTransaction(payload: $payload) }";
        var variables = new
        {
            payload = ByteUtil.Hex(tx.Serialize())
        };
        var request = new GraphQLHttpRequestWithAuth
        {
            Query = query,
            Variables = variables,
            Authentication = new AuthenticationHeaderValue("Bearer",Token()),
        };

        GraphQLResponse<StageTransactionResult> resp;
        try
        {
            resp = await _client.SendMutationAsync<StageTransactionResult>(request);
        }
        catch (Exception e)
        {
            _logger.LogError("{Msg}", e.Message);
            throw;
        }

        if (resp.Errors is null) return resp.Data.StageTransaction;

        var msg = resp.Errors.Aggregate("", (current, error) => current + error.Message + "\n");
        throw new GraphQLException(msg);
    }

    /// <summary>
    /// query transaction result by given <see cref="TxId"/>
    /// </summary>
    /// <param name="txId"><see cref="TxId"/></param>
    /// <returns><see cref="TransactionResult"/></returns>
    /// <exception cref="GraphQLException"></exception>
    public async Task<TransactionResult> Result(TxId txId)
    {
        var query = @"
query($txId: TxId!) {
  transaction {
    transactionResult(txId: $txId) {
      txStatus
      exceptionNames
      blockIndex
    }
  }
}
";
        var variables = new
        {
            txId = txId.ToHex()
        };
        var request = new GraphQLHttpRequestWithAuth
        {
            Query = query,
            Variables = variables,
            Authentication = new AuthenticationHeaderValue("Bearer",Token()),
        };

        GraphQLResponse<TransactionResultResponse> resp;
        try
        {
            resp = await _client.SendQueryAsync<TransactionResultResponse>(request);
        }
        catch (Exception e)
        {
            _logger.LogError("{Msg}", e.Message);
            throw;
        }

        if (resp.Errors is null) return resp.Data.Transaction.TransactionResult;

        var msg = resp.Errors.Aggregate("", (current, error) => current + error.Message + "\n");
        throw new GraphQLException(msg);
    }


    /// <summary>
    /// query transaction result by given transaction ids. the order of results is the same as the order of txIds.
    /// </summary>
    /// <param name="txIds">list of hex encoded tx ids</param>
    /// <returns><see cref="IReadOnlyList{TransactionResult}"/></returns>
    /// <exception cref="GraphQLException"></exception>
    public async Task<IReadOnlyList<TransactionResult>> Results(List<string> txIds)
    {
        var query = @"
query($txIds: [TxId]!) {
  transaction {
    transactionResults(txIds: $txIds) {
      txStatus
      exceptionNames
      blockIndex
    }
  }
}
";
        var variables = new
        {
            txIds,
        };
        var request = new GraphQLHttpRequestWithAuth
        {
            Query = query,
            Variables = variables,
            Authentication = new AuthenticationHeaderValue("Bearer",Token()),
        };

        GraphQLResponse<TransactionResultsResponse> resp;
        try
        {
            resp = await _client.SendQueryAsync<TransactionResultsResponse>(request);
        }
        catch (Exception e)
        {
            _logger.LogError("{Msg}", e.Message);
            throw;
        }

        if (resp.Errors is null) return resp.Data.Transaction.TransactionResults;

        var msg = resp.Errors.Aggregate("", (current, error) => current + error.Message + "\n");
        throw new GraphQLException(msg);
    }

    public async Task<int> Tip()
    {
        var query = @"query {
  nodeStatus {
    tip {
      index
    }
  }
}";
        var request = new GraphQLHttpRequestWithAuth
        {
            Query = query,
            Authentication = new AuthenticationHeaderValue("Bearer",Token()),
        };

        GraphQLResponse<NodeStatusResponse> resp;
        try
        {
            resp = await _client.SendQueryAsync<NodeStatusResponse>(request);
        }
        catch (Exception e)
        {
            _logger.LogError("{Msg}", e.Message);
            throw;
        }

        if (resp.Errors is null) return resp.Data.NodeStatus.Tip.Index;

        var msg = resp.Errors.Aggregate("", (current, error) => current + error.Message + "\n");
        throw new GraphQLException(msg);
    }

    /// <summary>
    /// Get patrol reward address next nonce from node
    /// </summary>
    /// <returns></returns>
    /// <exception cref="GraphQLException"></exception>
    public async Task<long> Nonce()
    {
        var query = @"query {
  nextTxNonce(address: ""0xCaD60f18b4Ba189f7f1c14E2267D9b20F5b16Ff5"")
}";
        var request = new GraphQLHttpRequestWithAuth
        {
            Query = query,
            Authentication = new AuthenticationHeaderValue("Bearer",Token()),
        };

        GraphQLResponse<NonceResponse> resp;
        try
        {
            resp = await _client.SendQueryAsync<NonceResponse>(request);
        }
        catch (Exception e)
        {
            _logger.LogError("{Msg}", e.Message);
            throw;
        }

        if (resp.Errors is null) return resp.Data.NextTxNonce;

        var msg = resp.Errors.Aggregate("", (current, error) => current + error.Message + "\n");
        throw new GraphQLException(msg);
    }

    public class GetAvatarResult
    {
        public StateQuery StateQuery;
    }

    public class StateQuery
    {
        public Avatar Avatar;
    }

    public class Avatar
    {
        public Address Address;
        public Address AgentAddress;
        public int Level;
    }

    public class UnloadGarageResult
    {
        public ActionQuery ActionQuery;
    }

    public class ActionQuery
    {
        public string UnloadFromMyGarages;
    }

    public class StageTransactionResult
    {
        public string StageTransaction;
    }

    public class TransactionResultResponse
    {
        public TransactionResultQuery Transaction;
    }

    public class TransactionResultQuery
    {
        public TransactionResult TransactionResult;
    }

    public class TransactionResultsResponse
    {
        public TransactionResultsQuery Transaction;
    }

    public class TransactionResultsQuery
    {
        public IReadOnlyList<TransactionResult> TransactionResults;
    }

    public class TransactionResult
    {
        public List<string>? exceptionNames;
        public TransactionStatus txStatus;
    }

    public class NodeStatusResponse
    {
        public TipResultQuery NodeStatus;
    }

    public class TipResultQuery
    {
        public TipResult Tip;
    }

    public class TipResult
    {
        public int Index;
    }

    public class NonceResponse
    {
        public long NextTxNonce;
    }

    public class GraphQLHttpRequestWithAuth : GraphQLHttpRequest {
        public AuthenticationHeaderValue? Authentication { get; set; }

        public override HttpRequestMessage ToHttpRequestMessage(GraphQLHttpClientOptions options, IGraphQLJsonSerializer serializer) {
            var r = base.ToHttpRequestMessage(options, serializer);
            r.Headers.Authorization = Authentication;
            return r;
        }
    }

    private string Token()
    {
        var token = new JwtSecurityToken(
            issuer: _issuer,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: _cred
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return jwt;
    }
}
