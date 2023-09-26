using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PatrolRewardService.Converters;

public class TxIdConverter : ValueConverter<TxId, string>
{
    public TxIdConverter() : base(v => v.ToHex(), v => TxId.FromHex(v))
    {
    }
}
