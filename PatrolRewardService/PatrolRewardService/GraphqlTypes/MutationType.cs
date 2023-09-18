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
                var rewardDbContext = context.Service<RewardDbContext>();
                var client = context.Service<NineChroniclesClient>();
                return Mutation.PutAvatar(rewardDbContext, client, avatarAddress, agentAddress);
            });
        descriptor
            .Field("putRewardPolicy")
            .UseServiceScope()
            .Argument("rewards", a => a.Type<NonNullType<ListType<RewardInputType>>>())
            .Argument("free", a => a.Type<NonNullType<BooleanType>>())
            .Argument("interval", a => a.Type<NonNullType<TimeSpanType>>())
            .Argument("activate", a => a.Type<NonNullType<BooleanType>>())
            .Argument("minimumLevel", a => a.Type<NonNullType<IntType>>())
            .Resolve(context =>
            {
                var rewardInputs = context.ArgumentValue<List<RewardInput>>("rewards");
                var rewards = rewardInputs.Select(r => r.ToReward()).ToList();
                var free = context.ArgumentValue<bool>("free");
                var interval = context.ArgumentValue<TimeSpan>("interval");
                var activate = context.ArgumentValue<bool>("activate");
                var minimumLevel = context.ArgumentValue<int>("minimumLevel");
                var rewardDbContext = context.Service<RewardDbContext>();
                return Mutation.PutClaimPolicy(rewardDbContext, rewards, free, interval, activate, minimumLevel);
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
                var rewardDbContext = context.Service<RewardDbContext>();
                var client = context.Service<NineChroniclesClient>();
                var signer = context.Service<Signer>();
                return Mutation.Claim(rewardDbContext, client, signer, avatarAddress, agentAddress);
            });
    }
}
