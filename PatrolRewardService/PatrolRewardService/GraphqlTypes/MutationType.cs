using Libplanet.Types.Tx;
using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field("putAvatar")
            .UseServiceScope()
            .Argument("avatarAddress", a => a.Type<NonNullType<AddressType>>())
            .Argument("agentAddress", a => a.Type<NonNullType<AddressType>>())
            .Resolve(context =>
            {
                var avatarAddress = context.ArgumentValue<string>("avatarAddress");
                var agentAddress = context.ArgumentValue<string>("agentAddress");
                var contextService = context.Service<ContextService>();
                var client = context.Service<NineChroniclesClient>();
                return Mutation.PutAvatar(contextService, client, avatarAddress, agentAddress);
            });
        descriptor
            .Field("putRewardPolicy")
            .UseServiceScope()
            .Argument("rewards", a => a.Type<NonNullType<ListType<RewardInputType>>>())
            .Argument("free", a => a.Type<NonNullType<BooleanType>>())
            .Argument("interval", a => a.Type<NonNullType<TimeSpanType>>())
            .Argument("activate", a => a.Type<NonNullType<BooleanType>>())
            .Argument("minimumLevel", a => a.Type<NonNullType<IntType>>())
            .Argument("password", a => a.Type<NonNullType<StringType>>())
            .Argument("maxLevel", a => a.Type<IntType>())
            .Argument("startedAt", a => a.Type<NonNullType<DateTimeType>>())
            .Argument("endedAt", a => a.Type<DateTimeType>())
            .Resolve(context =>
            {
                var rewardInputs = context.ArgumentValue<List<RewardInput>>("rewards");
                var free = context.ArgumentValue<bool>("free");
                var interval = context.ArgumentValue<TimeSpan>("interval");
                var activate = context.ArgumentValue<bool>("activate");
                var minimumLevel = context.ArgumentValue<int>("minimumLevel");
                var password = context.ArgumentValue<string>("password");
                var maxLevel = context.ArgumentValue<int?>("maxLevel");
                var startedAt = context.ArgumentValue<DateTime>("startedAt");
                var endedAt = context.ArgumentValue<DateTime?>("endedAt") ?? DateTime.MaxValue;
                var contextService = context.Service<ContextService>();
                var rewards = new List<RewardBaseModel>();
                foreach (var rewardInput in rewardInputs)
                {
                    var reward = Query.GetReward(contextService, rewardInput) ?? rewardInput.ToReward();
                    rewards.Add(reward);
                }
                return Mutation.PutClaimPolicy(contextService, rewards, free, interval, activate, minimumLevel, password, startedAt, endedAt, maxLevel);
            });
        descriptor
            .Field("claim")
            .UseServiceScope()
            .Argument("avatarAddress", a => a.Type<NonNullType<AddressType>>())
            .Argument("agentAddress", a => a.Type<NonNullType<AddressType>>())
            .Resolve(context =>
            {
                var avatarAddress = context.ArgumentValue<string>("avatarAddress");
                var agentAddress = context.ArgumentValue<string>("agentAddress");
                var contextService = context.Service<ContextService>();
                var client = context.Service<NineChroniclesClient>();
                var signer = context.Service<Signer>();
                return Mutation.Claim(contextService, client, signer, avatarAddress, agentAddress);
            });
        descriptor
            .Field("retryTransaction")
            .UseServiceScope()
            .Argument("txId", a => a.Type<NonNullType<TxIdType>>())
            .Argument("password", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var txId = context.ArgumentValue<TxId>("txId");
                var password = context.ArgumentValue<string>("password");
                var contextService = context.Service<ContextService>();
                var client = context.Service<NineChroniclesClient>();
                var signer = context.Service<Signer>();
                return Mutation.RetryTransaction(contextService, signer, client, txId, password);
            })
            .Type<TxIdType>();
        descriptor
            .Field("retryTransactions")
            .UseServiceScope()
            .Argument("startNonce", a => a.Type<NonNullType<IntType>>())
            .Argument("endNonce", a => a.Type<NonNullType<IntType>>())
            .Argument("password", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var startNonce = context.ArgumentValue<int>("startNonce");
                var endNonce = context.ArgumentValue<int>("endNonce");
                var password = context.ArgumentValue<string>("password");
                var contextService = context.Service<ContextService>();
                var client = context.Service<NineChroniclesClient>();
                var signer = context.Service<Signer>();
                return Mutation.RetryTransactions(contextService, signer, client, startNonce, endNonce, password);
            })
            .Type<ListType<TxIdType>>();
        descriptor
            .Field("replaceTransactions")
            .UseServiceScope()
            .Argument("startNonce", a => a.Type<NonNullType<IntType>>())
            .Argument("endNonce", a => a.Type<NonNullType<IntType>>())
            .Argument("password", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var startNonce = context.ArgumentValue<int>("startNonce");
                var endNonce = context.ArgumentValue<int>("endNonce");
                var password = context.ArgumentValue<string>("password");
                var contextService = context.Service<ContextService>();
                var client = context.Service<NineChroniclesClient>();
                var signer = context.Service<Signer>();
                return Mutation.ReplaceTransactions(contextService, signer, client, startNonce, endNonce, password);
            })
            .Type<ListType<TxIdType>>();
        descriptor
            .Field("stageTransactions")
            .UseServiceScope()
            .Argument("startNonce", a => a.Type<NonNullType<IntType>>())
            .Argument("endNonce", a => a.Type<NonNullType<IntType>>())
            .Argument("password", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var startNonce = context.ArgumentValue<int>("startNonce");
                var endNonce = context.ArgumentValue<int>("endNonce");
                var password = context.ArgumentValue<string>("password");
                var contextService = context.Service<ContextService>();
                var client = context.Service<NineChroniclesClient>();
                return Mutation.StageTransactions(contextService, client, startNonce, endNonce, password);
            })
            .Type<ListType<TxIdType>>();
    }
}
