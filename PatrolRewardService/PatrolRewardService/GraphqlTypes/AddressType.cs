using HotChocolate.Language;
using Libplanet.Crypto;

namespace PatrolRewardService.GraphqlTypes;

public class AddressType : ScalarType<string, StringValueNode>
{
    public AddressType() : base("address")
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        return ParseValue(resultValue);
    }

    protected override string ParseLiteral(StringValueNode valueSyntax)
    {
        return valueSyntax.Value;
    }

    protected override StringValueNode ParseValue(string runtimeValue)
    {
        return new StringValueNode(runtimeValue);
    }

    public override object? Serialize(object? runtimeValue)
    {
        if (runtimeValue is Address a) return a;

        return null;
    }
}
