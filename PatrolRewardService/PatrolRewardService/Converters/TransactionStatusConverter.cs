using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PatrolRewardService.Models;

namespace PatrolRewardService.Converters;

public class TransactionStatusConverter : ValueConverter<TransactionStatus, string>
{
    public TransactionStatusConverter() : base(
        v => v.ToString(),
        v => (TransactionStatus) Enum.Parse(typeof(TransactionStatus), v)
    )
    {
    }
}
