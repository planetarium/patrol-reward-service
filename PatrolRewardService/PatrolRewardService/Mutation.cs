using Lib9c;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Exceptions;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class Mutation
{
    public static async Task<AvatarModel> PutAvatar([Service] RewardDbContext rewardDbContext,
        [Service] NineChroniclesClient client,
        string avatarAddress, string agentAddress, bool save = true)
    {
        var avatarExist = true;
        var avatar = Query.GetAvatar(rewardDbContext, avatarAddress, agentAddress, true);
        if (avatar is null)
        {
            avatarExist = false;
            avatar = new AvatarModel
            {
                AvatarAddress = new Address(avatarAddress),
                AgentAddress = new Address(agentAddress),
                CreatedAt = DateTime.UtcNow
            };
        }

        var avatarState = await client.GetAvatar(avatarAddress);
        avatar.Level = avatarState.Level;
        if (save)
        {
            if (avatarExist)
                rewardDbContext.Avatars.Update(avatar);
            else
                rewardDbContext.Avatars.Add(avatar);
            await rewardDbContext.SaveChangesAsync();
        }

        return avatar;
    }

    public static async Task<int> PutClaimPolicy([Service] RewardDbContext rewardDbContext,
        List<RewardBaseModel> rewards, bool free, TimeSpan interval, bool activate, int minimumLevel, int? maxLevel = null)
    {
        var policy = await rewardDbContext
            .RewardPolicies
            .Include(r => r.Rewards)
            .FirstOrDefaultAsync(r => r.Free == free && r.Activate == activate && r.MinimumLevel == minimumLevel);
        if (policy is null)
        {
            policy = new RewardPolicyModel
            {
                Rewards = rewards,
                Free = free,
                MinimumRequiredInterval = interval,
                Activate = activate,
                MinimumLevel = minimumLevel,
                MaxLevel = maxLevel,
            };
            await rewardDbContext.RewardPolicies.AddAsync(policy);
        }
        else
        {
            policy.Rewards.Clear();
            policy.Rewards = rewards;
            policy.MaxLevel = maxLevel;
            policy.MinimumRequiredInterval = interval;
            rewardDbContext.RewardPolicies.Update(policy);
        }

        await rewardDbContext.SaveChangesAsync();
        return policy.Id;
    }

    public static async Task<string> Claim(
        [Service] RewardDbContext rewardDbContext,
        [Service] NineChroniclesClient client,
        [Service] Signer signer,
        string avatarAddress,
        string agentAddress,
        bool free = true
    )
    {
        // Check registered player.
        var avatar = Query.GetAvatar(rewardDbContext, avatarAddress, agentAddress, true);
        if (avatar is null) throw new GraphQLException("Avatar not found. register avatar first.");

        // Check duplicate claim.
        var transaction = await rewardDbContext
            .Transactions
            .FirstOrDefaultAsync(t => t.AvatarAddress == avatar.AvatarAddress && t.ClaimCount == avatar.ClaimCount);
        if (transaction is not null) throw new GraphQLException("pending request exist.");

        // Check player level
        var avatarState = await client.GetAvatar(avatarAddress);
        avatar.Level = avatarState.Level;

        // Check claim policy
        var policy = Query.GetPolicy(rewardDbContext, free, avatarState.Level);

        // check claim interval 
        var lastClaimedAt = avatar.LastClaimedAt ?? avatar.CreatedAt;
        var now = DateTime.UtcNow;
        var diff = now - lastClaimedAt;

        if (diff >= policy.MinimumRequiredInterval)
            // save pending tx for continuation.
            transaction = new TransactionModel
            {
                Avatar = avatar,
                CreatedAt = now,
                ClaimCount = avatar.ClaimCount
            };
        else
            throw new ClaimIntervalException($"required minimum interval time {policy.MinimumRequiredInterval}.");

        // prepare claim.
        avatar.LastClaimedAt = now;
        ClaimModel claim = new()
        {
            Policy = policy,
            Avatar = avatar,
            CreatedAt = now,
            Transaction = transaction
        };
        foreach (var reward in policy.Rewards)
        {
            var claimReward = reward.CalculateReward(claim, diff, now);
            claim.Garages.Add(claimReward);
        }

        policy.Claims.Add(claim);

        // prepare action plain value.
        var memo = $"patrol reward {avatarAddress} / {avatar.ClaimCount}";
        var action = claim.ToAction(avatarState.Address, avatarState.AgentAddress, memo);
        long nonce = 0L;
        try
        {
            nonce = await rewardDbContext.Transactions.Select(p => p.Nonce).MaxAsync() + 1;
        }
        catch (InvalidOperationException)
        {
            //pass
        }
        var tx = signer.Sign(nonce, new[] {action}, 1 * Currencies.Mead, 4L, now);
        transaction.TxId = tx.Id;
        transaction.Payload = Convert.ToBase64String(tx.Serialize());
        transaction.Nonce = nonce;
        transaction.Avatar.ClaimCount++;
        transaction.Claim = claim;
        transaction.GasLimit = tx.GasLimit;
        transaction.Gas = 1;
        await client.StageTx(tx);
        transaction.Result = TransactionStatus.STAGING;
        await rewardDbContext.Transactions.AddAsync(transaction);
        await rewardDbContext.SaveChangesAsync();
        return claim.TxId.ToHex();
    }
}
