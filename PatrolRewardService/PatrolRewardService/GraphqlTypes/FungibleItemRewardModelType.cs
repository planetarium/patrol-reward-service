using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class FungibleItemRewardModelType : ObjectType<FungibleItemRewardModel>
{
    protected override void Configure(IObjectTypeDescriptor<FungibleItemRewardModel> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor
            .Field(f => f.ItemId)
            .Type<IntType>();
        descriptor
            .Field(f => f.FungibleId)
            .Type<StringType>();
        descriptor
            .Field(f => f.PerInterval)
            .Type<IntType>();
        descriptor
            .Field(f => f.RewardInterval)
            .Type<TimeSpanType>();
        // descriptor
        //     .Field(f => f.RewardPolicies)
        //     .Ignore();
        // descriptor
        //     .Field(f => f.CalculateReward)
        //     .Ignore();
        // descriptor
        //     .Field(f => f.CalculateCount)
        //     .Ignore();
        // descriptor
        //     .Field(f => f.CalculateRate)
        //     .Ignore();
    }
}
