using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;

namespace PatrolRewardService.Models;

public class PlayerModel
{
    [Key] public Address AvatarAddress { get; set; }
    [Required] public Address AgentAddress { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastClaimedAt { get; set; }
}
