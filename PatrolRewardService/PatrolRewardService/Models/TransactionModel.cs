using System.ComponentModel.DataAnnotations;
using Libplanet.Crypto;
using Libplanet.Types.Tx;

namespace PatrolRewardService.Models;

public class TransactionModel
{
    [Key] public TxId TxId { get; set; }

    public Address AvatarAddress { get; set; }

    public AvatarModel Avatar { get; set; }

    public long Nonce { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Payload { get; set; }

    public int ClaimId { get; set; }
    public ClaimModel Claim { get; set; }
    public int ClaimCount { get; set; }

    public TransactionStatus Result { get; set; } = TransactionStatus.CREATED;

    public int? Gas { get; set; }
    public long? GasLimit { get; set; }
    public string? ExceptionName { get; set; }
}

public enum TransactionStatus
{
    CREATED,

    // from TxResultType.txStatus
    INVALID,
    STAGING,
    SUCCESS,
    FAILURE
}