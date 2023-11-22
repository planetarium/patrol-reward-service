using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class ClaimModelTest
{
    [Fact]
    public void ToClaimItems()
    {
        var interval = TimeSpan.FromHours(1);
        var favReward = new FungibleAssetValueRewardModel
        {
            PerInterval = 900,
            Currency = "CRYSTAL",
            RewardInterval = interval,
        };
        var itemReward = new FungibleItemRewardModel
        {
            PerInterval = 6,
            FungibleId = "fungibleId",
            ItemId = 800201,
            RewardInterval = interval,
        };
        var policyModel = new RewardPolicyModel
        {
            Activate = true,
            Free = true,
            MaxLevel = 150,
            MinimumLevel = 1,
            MinimumRequiredInterval = interval,
            Rewards = new List<RewardBaseModel>
            {
                favReward,
                itemReward,
            }
        };
        var claimModel = new ClaimModel
        {
            Policy = policyModel
        };
        foreach (var reward in policyModel.Rewards)
        {
            var claimReward = reward.CalculateReward(claimModel, interval, DateTime.UtcNow, interval);
            claimModel.Garages.Add(claimReward);
        }

        var memo = "memo";
        var avatarAddress = new PrivateKey().ToAddress();
        var agentAddress = new PrivateKey().ToAddress();
        var action = claimModel.ToClaimItems(avatarAddress, agentAddress, memo);
        Assert.Equal(memo, action.Memo);
        foreach (var (address, fungibleAssetValues) in action.ClaimData)
        {
            Assert.Equal(avatarAddress, address);
            foreach (var fungibleAssetValue in fungibleAssetValues)
            {
                var currency = fungibleAssetValue.Currency;
                if (Currencies.IsWrappedCurrency(currency))
                {
                    Assert.Equal(900 * currency, fungibleAssetValue);
                    Assert.Equal(900 * Currencies.Crystal, FungibleAssetValue.FromRawValue(Currencies.GetUnwrappedCurrency(currency), fungibleAssetValue.RawValue));
                }
                else
                {
                    Assert.Equal(6 * Currencies.GetItemCurrency(800201, false), fungibleAssetValue);
                    var (tradable, itemId) = Currencies.ParseItemCurrency(currency);
                    Assert.False(tradable);
                    Assert.Equal(800201, itemId);
                }
            }
        }
    }
}
