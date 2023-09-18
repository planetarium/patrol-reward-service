using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class RewardInput
{
    public int? PerInterval { get; set; } = null;
    public TimeSpan? RewardInterval { get; set; } = null;
    public string? FungibleId { get; set; } = null;
    public int? ItemId { get; set; } = null;
    public string? Currency { get; set; } = null;
    public string? Ticker { get; set; } = null;

    public RewardBaseModel ToReward()
    {
        var perInterval = PerInterval!.Value;
        var rewardInterval = RewardInterval!.Value;
        if (string.IsNullOrEmpty(Currency) && string.IsNullOrEmpty(Ticker))
            return new FungibleItemRewardModel
            {
                PerInterval = perInterval,
                RewardInterval = rewardInterval,
                FungibleId = FungibleId!,
                ItemId = ItemId!.Value
            };

        return new FungibleAssetValueRewardModel
        {
            Currency = Currency!,
            Ticker = Ticker!,
            PerInterval = perInterval,
            RewardInterval = rewardInterval
        };
    }
}

public class RewardInputType : InputObjectType<RewardInput>
{
}
