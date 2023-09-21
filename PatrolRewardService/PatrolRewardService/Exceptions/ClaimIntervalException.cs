namespace PatrolRewardService.Exceptions;

[Serializable]
public class ClaimIntervalException : InvalidOperationException
{
    public ClaimIntervalException(string msg) : base(msg)
    {
    }
}
