using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PatrolRewardService.Models;

public class RewardBaseModel
{
    public int Id { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public int PerInterval { get; set; }

    [Required] public TimeSpan RewardInterval { get; set; }

    public List<RewardPolicyModel> RewardPolicies { get; } = new();

    public GarageModel CalculateReward(ClaimModel claim, TimeSpan diff, DateTime dateTime)
    {
        return new GarageModel
        {
            Reward = this,
            Claim = claim,
            Count = CalculateCount(diff),
            CreatedAt = dateTime
        };
    }

    public int CalculateRate(TimeSpan diff)
    {
        var total = TimeSpan.FromDays(1);
        if (diff >= total) diff = total;
        var rate = (int) (diff / RewardInterval);
        return rate;
    }

    public int CalculateCount(TimeSpan diff)
    {
        var rate = CalculateRate(diff);
        return PerInterval * rate;
    }
}
