using System.ComponentModel.DataAnnotations.Schema;

namespace PatrolRewardService.Models;

public class GarageModel
{
    public int Id { get; set; }

    public int ClaimId { get; set; }

    public ClaimModel Claim { get; set; }

    public int RewardId { get; set; }

    public RewardBaseModel Reward { get; set; }

    public int Count { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
