using Libplanet.Types.Tx;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class Query
{
    public AvatarModel? GetAvatar(ContextService contextService, string avatarAddress,
        string agentAddress, bool track = false)
    {
        return contextService.GetAvatar(avatarAddress, agentAddress, track);
    }

    public static RewardPolicyModel GetPolicy(ContextService contextService, bool free, int level)
    {
        return contextService.GetPolicy(free, level);
    }

    public static RewardBaseModel? GetReward(ContextService contextService, RewardInput rewardInput)
    {
        return contextService.GetReward(rewardInput);
    }

    public static async Task<TransactionModel?> GetTransaction(ContextService contextService, TxId txId)
    {
        return await contextService.GetTransaction(txId);
    }

    public static IQueryable<TransactionModel> Transactions(ContextService contextService)
    {
        return contextService.Transactions();
    }

    /// <summary>
    /// Retrieves the count of pending transactions from a given context service.
    /// </summary>
    /// <param name="contextService">The context service to retrieve the pending transactions count from.</param>
    /// <returns>The count of pending transactions as an asynchronous operation.</returns>
    public static async Task<int> PendingTxCount(ContextService contextService)
    {
        return await contextService.PendingTxCount();
    }
}
