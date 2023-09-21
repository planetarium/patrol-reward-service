using System.Runtime.InteropServices;
using PatrolRewardService.Models;
using Xunit;

namespace PatrolRewardService.Tests;

public class RewardModelTest
{
    [Theory]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    [InlineData(5, 1)]
    public void CalculateCount(int diff, int expectedCount)
    {
        var interval = TimeSpan.FromHours(4);
        var itemReward = new FungibleItemRewardModel
        {
            PerInterval = 1,
            RewardInterval = interval
        };
        var favReward = new FungibleAssetValueRewardModel
        {
            PerInterval = 1,
            RewardInterval = interval,
        };

        foreach (var reward in new RewardBaseModel[] { itemReward, favReward })
        {
            Assert.Equal(expectedCount, reward.CalculateCount(TimeSpan.FromHours(diff), interval));
        }
    }
}
