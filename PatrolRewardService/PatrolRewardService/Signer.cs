using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Microsoft.Extensions.Options;

namespace PatrolRewardService;

public class Signer
{
    private readonly BlockHash _genesisHash;
    private readonly PrivateKey _privateKey;

    public Signer(IOptions<SignerOptions> options)
    {
        var option = options.Value;
        _privateKey = new PrivateKey(option.PrivateKey);
        _genesisHash = BlockHash.FromString(option.GenesisHash);
    }

    public Transaction Sign(long nonce, IEnumerable<IAction> actions, FungibleAssetValue? maxGasPrice, long? gasLimit,
        DateTimeOffset? timestamp = null)
    {
        return Transaction.Create(
            nonce,
            _privateKey,
            _genesisHash,
            actions.Select(a => a.PlainValue),
            maxGasPrice,
            gasLimit,
            timestamp: timestamp
        );
    }
}