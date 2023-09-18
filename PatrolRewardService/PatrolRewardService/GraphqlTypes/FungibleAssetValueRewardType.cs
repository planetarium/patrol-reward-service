using System.Runtime.CompilerServices;
using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class FungibleAssetValueRewardType : ObjectType<FungibleAssetValueRewardModel>
{
    protected override void Configure(IObjectTypeDescriptor<FungibleAssetValueRewardModel> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(f => f.Currency)
            .Type<StringType>();
        descriptor.Field(f => f.PerInterval)
            .Type<IntType>();
        descriptor.Field(f => f.RewardInterval)
            .Type<TimeSpanType>();
    }
}
