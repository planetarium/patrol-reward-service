using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class AvatarModelType : ObjectType<AvatarModel>
{
    protected override void Configure(IObjectTypeDescriptor<AvatarModel> descriptor)
    {
        descriptor
            .Field(f => f.AvatarAddress)
            .Type<NonNullType<AddressType>>();
        descriptor
            .Field(f => f.AgentAddress)
            .Type<NonNullType<AddressType>>();
        descriptor
            .Field(f => f.Claims)
            .Ignore();
        descriptor
            .Field(f => f.TransactionModels)
            .Ignore();
    }
}
