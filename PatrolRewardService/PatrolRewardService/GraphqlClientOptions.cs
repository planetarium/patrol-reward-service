namespace PatrolRewardService;

public class GraphqlClientOptions
{
    public const string GraphqlClientConfig = "GraphqlClientConfig";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }
}