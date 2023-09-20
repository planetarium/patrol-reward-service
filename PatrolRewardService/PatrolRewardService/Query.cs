using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
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
}
