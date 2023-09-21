namespace PatrolRewardService.GraphqlTypes;

public class QueryType : ObjectType<Query>
{
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
    }
}
