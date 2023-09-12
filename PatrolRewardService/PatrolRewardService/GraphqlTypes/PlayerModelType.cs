using HotChocolate.Types;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class PlayerModelType : ObjectType<PlayerModel>
{
    protected override void Configure(IObjectTypeDescriptor<PlayerModel> descriptor)
    {
        descriptor
            .Field(f => f.AvatarAddress)
            .Type<AddressType>();
        descriptor
            .Field(f => f.AgentAddress)
            .Type<AddressType>();
    }
}
