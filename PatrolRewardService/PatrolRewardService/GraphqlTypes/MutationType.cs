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
            .Resolve(context =>
            {
                var rewardInputs = context.ArgumentValue<List<RewardInput>>("rewards");
                var free = context.ArgumentValue<bool>("free");
                var interval = context.ArgumentValue<TimeSpan>("interval");
                var activate = context.ArgumentValue<bool>("activate");
                var minimumLevel = context.ArgumentValue<int>("minimumLevel");
                var password = context.ArgumentValue<string>("password");
                var maxLevel = context.ArgumentValue<int?>("maxLevel");
                var contextService = context.Service<ContextService>();
                var rewards = new List<RewardBaseModel>();
                foreach (var rewardInput in rewardInputs)
                {
                    var reward = Query.GetReward(contextService, rewardInput) ?? rewardInput.ToReward();
                    rewards.Add(reward);
                }
                return Mutation.PutClaimPolicy(contextService, rewards, free, interval, activate, minimumLevel, password, maxLevel);
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
    }
}
