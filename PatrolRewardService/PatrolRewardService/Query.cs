using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class Query
{
    public static AvatarModel? GetAvatar([Service] RewardDbContext rewardDbContext, string avatarAddress,
        string agentAddress, bool track = false)
    {
        IQueryable<AvatarModel> baseQuery = rewardDbContext.Avatars;
        if (!track)
        {
            baseQuery = baseQuery.AsNoTracking();
        }
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

    public static RewardBaseModel? GetReward([Service] RewardDbContext rewardDbContext, RewardInput rewardInput)
    {
        if (rewardInput.IsItemReward())
        {
            return rewardDbContext
                .FungibleItemRewards
                .Include(r => r.RewardPolicies)
                .FirstOrDefault(p =>
                    p.RewardInterval == rewardInput.RewardInterval
                    && p.PerInterval == rewardInput.PerInterval
                    && p.FungibleId == rewardInput.FungibleId
                    && p.ItemId == rewardInput.ItemId
                );
        }

        return rewardDbContext
            .FungibleAssetValueRewards
            .Include(r => r.RewardPolicies)
            .FirstOrDefault(p =>
                p.RewardInterval == rewardInput.RewardInterval
                && p.PerInterval == rewardInput.PerInterval
                && p.Currency == rewardInput.Currency
                && p.Ticker == rewardInput.Ticker
            );
    }
}
