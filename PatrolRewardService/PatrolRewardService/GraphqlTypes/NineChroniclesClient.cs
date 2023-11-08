using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.Extensions.Options;
using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class NineChroniclesClient
{
    private readonly GraphQLHttpClient _client;
    private readonly ILogger<NineChroniclesClient> _logger;

    public NineChroniclesClient(IOptions<GraphqlClientOptions> options, ILoggerFactory loggerFactory)
    {
        var graphqlClientOptions = options.Value;
        var clientOptions = new GraphQLHttpClientOptions
        {
            EndPoint = new Uri($"{graphqlClientOptions.Host}:{graphqlClientOptions.Port}/graphql")
        };
        _client = new GraphQLHttpClient(clientOptions, new NewtonsoftJsonSerializer());
        _logger = loggerFactory.CreateLogger<NineChroniclesClient>();
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
        var request = new GraphQLRequest
        {
            Query = query,
            Variables = new
            {
                avatarAddress
            }
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
        var request = new GraphQLRequest
        {
            Query = query,
            Variables = variables
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
        var request = new GraphQLRequest
        {
            Query = query,
            Variables = variables
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

    public async Task<int> Tip()
    {
        var query = @"query {
  nodeStatus {
    tip {
      index
    }
  }
}";
        var request = new GraphQLRequest
        {
            Query = query,
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
}
