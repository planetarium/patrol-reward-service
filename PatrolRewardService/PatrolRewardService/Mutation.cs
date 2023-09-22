using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Exceptions;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class Mutation
{
    public static async Task<AvatarModel> PutAvatar(
        ContextService contextService,
        [Service] NineChroniclesClient client,
        string avatarAddress,
        string agentAddress,
        bool save = true
    )
    {
        var avatarState = await client.GetAvatar(avatarAddress);
        return await contextService.PutAvatar(avatarState, avatarAddress, agentAddress, save);
    }

    public static async Task<int> PutClaimPolicy(ContextService contextService,
        List<RewardBaseModel> rewards, bool free, TimeSpan interval, bool activate, int minimumLevel, string password, int? maxLevel = null)
    {
        return await contextService.PutClaimPolicy(rewards, free, interval, activate, minimumLevel, password, maxLevel);
    }

    public static async Task<string> Claim(
        ContextService contextService,
        [Service] NineChroniclesClient client,
        [Service] Signer signer,
        string avatarAddress,
        string agentAddress,
        bool free = true
    )
    {
        // Check registered player.
        var avatar = contextService.GetAvatar(avatarAddress, agentAddress, true);
        if (avatar is null) throw new GraphQLException("Avatar not found. register avatar first.");

        // Check duplicate claim.
        var transaction = await contextService.GetTransaction(avatar);
        if (transaction is not null) throw new GraphQLException("pending request exist.");

        // Check player level
        var avatarState = await client.GetAvatar(avatarAddress);
        avatar.Level = avatarState.Level;

        // Check claim policy
        var policy = contextService.GetPolicy(free, avatarState.Level);

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
        };
        claim.Transactions.Add(transaction);
        var maxInterval = policy.MinimumRequiredInterval;
        foreach (var reward in policy.Rewards)
        {
            var claimReward = reward.CalculateReward(claim, diff, now, maxInterval);
            claim.Garages.Add(claimReward);
        }

        policy.Claims.Add(claim);

        // prepare action plain value.
        var memo = $"patrol reward {avatarAddress} / {avatar.ClaimCount}";
        var action = claim.ToAction(avatarState.Address, avatarState.AgentAddress, memo);
        long nonce = await contextService.GetNonce();
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
        await contextService.InsertTransaction(transaction);
        return transaction.TxId.ToHex();
    }

    public static async Task<TxId?> RetryTransaction(
        ContextService contextService,
        [Service] Signer signer,
        [Service] NineChroniclesClient client,
        TxId txId,
        string password
    )
    {
        return await contextService.RetryTransaction(signer, client, txId, password);
    }

    public static async Task<DateTime?> SetAvatarTimestamp(ContextService contextService, string avatarAddress, DateTime? timeStamp = null)
    {
        return await contextService.SetAvatarTimestmap(avatarAddress, timeStamp);
    }

    public static async Task<bool> DeleteAvatar(ContextService contextService, string avatarAddress)
    {
        return await contextService.DeleteAvatar(avatarAddress);
    }
}
