using PatrolRewardService.Models;

namespace PatrolRewardService.GraphqlTypes;

public class TransactionType : ObjectType<TransactionModel>
{
    protected override void Configure(IObjectTypeDescriptor<TransactionModel> descriptor)
    {
        descriptor
            .Field(f => f.AvatarAddress)
            .Type<NonNullType<AddressType>>();
        descriptor
            .Field(f => f.Avatar)
            .Type<NonNullType<AvatarModelType>>();
        descriptor
            .Field(f => f.TxId)
            .Type<NonNullType<TxIdType>>();
        descriptor
            .Field(f => f.Claim)
            .Ignore();
    }
}
