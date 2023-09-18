using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;

namespace PatrolRewardService.Models;

public class AvatarModel
{
    [Key] public Address AvatarAddress { get; set; }
    [Required] public Address AgentAddress { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastClaimedAt { get; set; }

    [Required] public int Level { get; set; } = 0;

    public List<ClaimModel> Claims { get; } = new();

    public List<TransactionModel> TransactionModels { get; } = new();

    public int ClaimCount { get; set; } = 0;
}
