namespace PatrolRewardService.Models;

public class FungibleAssetValueRewardModel : RewardBaseModel
{
    public string Currency { get; set; }
    public string Ticker { get; set; } = null!;
}
