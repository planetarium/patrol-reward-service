namespace PatrolRewardService;

public class SignerOptions
{
    public const string SignerConfig = "SignerConfig";

    public string PrivateKey { get; set; } = string.Empty;

    public string GenesisHash { get; set; } = string.Empty;
}