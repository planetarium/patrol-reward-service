using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using Lib9c;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Action.Garages;
using PatrolRewardService.Exceptions;

namespace PatrolRewardService.Models;

public class ClaimModel
{
    public int Id { get; set; }

    public Address AvatarAddress { get; set; }
    public AvatarModel Avatar { get; set; }

    public int PolicyId { get; set; }

    public RewardPolicyModel Policy { get; set; }

    public List<GarageModel> Garages { get; } = new();

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TransactionModel> Transactions { get; } = new();

    public UnloadFromMyGarages ToAction(Address avatarAddress, Address agentAddress, string? memo = null)
    {
        List<(Address, FungibleAssetValue)> fungibleAssetValues = new();
        List<(HashDigest<SHA256>, int)> fungibleIdAndCounts = new();
        foreach (var garage in Garages)
            switch (garage.Reward)
            {
                case FungibleItemRewardModel fungibleItemReward:
                    fungibleIdAndCounts.Add(
                        (HashDigest<SHA256>.FromString(fungibleItemReward.FungibleId), garage.Count));
                    break;
                case FungibleAssetValueRewardModel fungibleAssetValueReward:
                    switch (fungibleAssetValueReward.Currency)
                    {
                        case "CRYSTAL":
                            fungibleAssetValues.Add((agentAddress, Currencies.Crystal * garage.Count));
                            break;
                        default:
                            throw new InvalidCurrencyException(
                                $"{fungibleAssetValueReward.Currency} does not support.");
                    }

                    break;
            }

        if (!fungibleIdAndCounts.Any() && !fungibleAssetValues.Any()) throw new ClaimIntervalException("no reward available. please wait more time.");
        return new UnloadFromMyGarages(avatarAddress, fungibleIdAndCounts: fungibleIdAndCounts,
            fungibleAssetValues: fungibleAssetValues, memo: memo);
    }

    public ClaimItems ToClaimItems(Address avatarAddress, Address agentAddress, string? memo = null)
    {
        var fungibleAssetValues = new List<FungibleAssetValue>();
        foreach (var garage in Garages)
        {
            switch (garage.Reward)
            {
#pragma warning disable CS0618
                case FungibleItemRewardModel fungibleItemReward:
                    fungibleAssetValues.Add(garage.Count * Currency.Legacy($"Item_NT_{fungibleItemReward.ItemId}", decimalPlaces: 0, minters: null));
                    break;
                case FungibleAssetValueRewardModel fungibleAssetValueReward:
                    switch (fungibleAssetValueReward.Currency)
                    {
                        case "CRYSTAL":
                            var currency = Currencies.Crystal;
                            var ticker = $"FAV__{currency.Ticker}";
                            var wrappedCurrency = Currency.Legacy(ticker, currency.DecimalPlaces, null);
#pragma warning restore CS0618
                            fungibleAssetValues.Add(wrappedCurrency * garage.Count);
                            break;
                        default:
                            throw new InvalidCurrencyException(
                                $"{fungibleAssetValueReward.Currency} does not support.");
                    }
                    break;
            }
        }
        if (!fungibleAssetValues.Any()) throw new ClaimIntervalException("no reward available. please wait more time.");
        return new ClaimItems(new List<(Address, IReadOnlyList<FungibleAssetValue>)>
        {
            (avatarAddress, fungibleAssetValues)
        });
    }
}
