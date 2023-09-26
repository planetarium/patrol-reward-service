namespace PatrolRewardService.Models;

public class FungibleItemRewardModel : RewardBaseModel
{
    public string FungibleId { get; set; }

    public int ItemId { get; set; }
}
