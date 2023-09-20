using HotChocolate.Language;
using Libplanet.Types.Tx;

namespace PatrolRewardService.GraphqlTypes;

public class TxIdType: ScalarType<TxId, StringValueNode>
{
    public TxIdType() : base("txId")
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return ParseValue(resultValue);
    }

    protected override TxId ParseLiteral(StringValueNode valueSyntax)
    {
        return TxId.FromHex(valueSyntax.Value);
    }

    protected override StringValueNode ParseValue(TxId runtimeValue)
    {
        return new StringValueNode(runtimeValue.ToHex());
    }

    public override object? Serialize(object? runtimeValue)
    {
        if (runtimeValue is TxId txId) return txId;

        return null;
    }
}
