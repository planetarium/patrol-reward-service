using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PatrolRewardService;

public class HeadlessNodeHealthCheck : IHealthCheck
{
    private volatile bool _ready;

    public bool ConnectCompleted
    {
        get => _ready;
        set => _ready = value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (ConnectCompleted)
        {
            return Task.FromResult(HealthCheckResult.Healthy("headless node ready"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("headless node not ready"));
    }
}
