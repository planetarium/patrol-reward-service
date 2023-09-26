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

    public GarageModel CalculateReward(ClaimModel claim, TimeSpan diff, DateTime dateTime, TimeSpan maxInterval)
    {
        return new GarageModel
        {
            Reward = this,
            Claim = claim,
            Count = CalculateCount(diff, maxInterval),
            CreatedAt = dateTime
        };
    }

    public int CalculateRate(TimeSpan diff, TimeSpan maxInterval)
    {
        if (diff >= maxInterval) diff = maxInterval;
        var rate = (int) (diff / RewardInterval);
        return rate;
    }

    public int CalculateCount(TimeSpan diff, TimeSpan maxInterval)
    {
        var rate = CalculateRate(diff, maxInterval);
        return PerInterval * rate;
    }
}
