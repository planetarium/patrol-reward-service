namespace PatrolRewardService;

public class GraphqlClientOptions
{
    public const string GraphqlClientConfig = "GraphqlClientConfig";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string JwtSecret { get; set; } = String.Empty;

    public string JwtIssuer { get; set; } = string.Empty;
}
