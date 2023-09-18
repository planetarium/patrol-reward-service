using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class Query
{
    public static AvatarModel? GetAvatar([Service] RewardDbContext rewardDbContext, string avatarAddress,
        string agentAddress)
    {
        IQueryable<AvatarModel> baseQuery = rewardDbContext.Avatars.AsNoTracking();
        return baseQuery.FirstOrDefault(p =>
            p.AvatarAddress == new Address(avatarAddress) && p.AgentAddress == new Address(agentAddress));
    }

    public static RewardPolicyModel GetPolicy([Service] RewardDbContext rewardDbContext, bool free, int level)
    {
        var policies = rewardDbContext
            .RewardPolicies
            .Include(p => p.Rewards)
            .Where(p => p.Activate && p.Free == free)
            .OrderByDescending(p => p.MinimumLevel)
            .ToList();
        foreach (var policy in policies)
            if (policy.MinimumLevel <= level)
                return policy;

        return policies.Last();
    }
}
