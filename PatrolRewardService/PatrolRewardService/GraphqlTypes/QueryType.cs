using HotChocolate.Types;
using Libplanet.Crypto;

namespace PatrolRewardService;

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(f => f.GetPlayer(default!, default!))
            .Argument("avatarAddress", a => a.Type<NonNullType<AddressType>>())
            .Type<PlayerModelType>();
    }
}
