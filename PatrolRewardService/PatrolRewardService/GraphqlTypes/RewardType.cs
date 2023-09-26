using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class RewardType : UnionType<RewardBaseModel>
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Type<FungibleAssetValueRewardType>();
        descriptor.Type<FungibleItemRewardModelType>();
    }
}
