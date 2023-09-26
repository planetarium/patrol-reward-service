using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PatrolRewardService.Converters;

public class AddressConverter : ValueConverter<Address, string>
{
    public AddressConverter() : base(v => v.ToString(), v => new Address(v))
    {
    }
}
