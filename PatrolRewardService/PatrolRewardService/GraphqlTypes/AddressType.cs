using HotChocolate.Language;
using HotChocolate.Types;
using Libplanet.Crypto;

namespace PatrolRewardService;

public class AddressType : ScalarType<string, StringValueNode>
{
    public AddressType() : base("Address")
    {
    }

    public override IValueNode ParseResult(object? resultValue) => ParseValue(resultValue);
    protected override string ParseLiteral(StringValueNode valueSyntax) => valueSyntax.Value;
    protected override StringValueNode ParseValue(string runtimeValue) => new(runtimeValue);
    public override object? Serialize(object? runtimeValue)
    {
        if (runtimeValue is Address a)
        {
            return a;
        }

        return null;
    }
}
