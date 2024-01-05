using Libplanet.Types.Tx;

namespace PatrolRewardService.GraphqlTypes;

public class QueryType : ObjectType<Query>
{
    /// <summary>
    /// Configures the ObjectTypeDescriptor for the Query object.
    /// </summary>
    /// <param name="descriptor">The ObjectTypeDescriptor to configure.</param>
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(f => f.GetAvatar(default!, default!, default!, default!))
            .Argument("avatarAddress", a => a.Type<NonNullType<AddressType>>())
            .Argument("agentAddress", a => a.Type<NonNullType<AddressType>>())
            .Type<AvatarModelType>();
        descriptor.Field(f => Query.GetPolicy(default!, default!, default!))
            .Argument("free", a => a.Type<NonNullType<BooleanType>>())
            .Argument("level", a => a.Type<NonNullType<IntType>>())
            .Type<RewardPolicyType>();
        descriptor.Field(f => Query.GetReward(default!, default!))
            .Argument("rewardInput", a => a.Type<RewardInputType>())
            .Type<RewardType>();
        descriptor.Field("transaction")
            .Argument("txId", a => a.Type<NonNullType<TxIdType>>())
            .Resolve(context =>
            {
                var txId = context.ArgumentValue<TxId>("txId");
                var contextService = context.Service<ContextService>();
                return Query.GetTransaction(contextService, txId);
            })
            .Type<TransactionType>();
        descriptor.Field("transactions")
            .Type<ListType<TransactionType>>()
            .Resolve(context =>
            {
                var contextService = context.Service<ContextService>();
                return Query.Transactions(contextService);
            })
            .UseFiltering();
        descriptor.Field("pendingTxCount")
            .Description("Retrieves the count of pending transactions")
            .Resolve(context =>
            {
                var contextService = context.Service<ContextService>();
                return Query.PendingTxCount(contextService);
            })
            .Type<IntType>();
    }
}
