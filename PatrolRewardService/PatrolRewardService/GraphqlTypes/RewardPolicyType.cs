using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class RewardPolicyType : ObjectType<RewardPolicyModel>
{
    protected override void Configure(IObjectTypeDescriptor<RewardPolicyModel> descriptor)
    {
        descriptor
            .Field(f => f.Claims)
            .Ignore();
        descriptor
            .Field(f => f.Rewards)
            .Type<NonNullType<ListType<NonNullType<RewardType>>>>();
    }
}
