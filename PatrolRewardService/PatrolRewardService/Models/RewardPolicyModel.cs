using System.ComponentModel.DataAnnotations.Schema;

namespace PatrolRewardService.Models;

public class RewardPolicyModel
{
    public int Id { get; set; }

    public bool Activate { get; set; }

    public bool Free { get; set; }

    public int MinimumLevel { get; set; }

    public TimeSpan MinimumRequiredInterval { get; set; }

    public List<RewardBaseModel> Rewards { get; set; } = new();

    public List<ClaimModel> Claims { get; set; } = new();

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
