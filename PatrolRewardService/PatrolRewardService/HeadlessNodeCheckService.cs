using PatrolRewardService.GraphqlTypes;

namespace PatrolRewardService;

public class HeadlessNodeCheckService : BackgroundService
{
    private readonly HeadlessNodeHealthCheck _healthCheck;
    private readonly NineChroniclesClient _graphqlClient;

    public HeadlessNodeCheckService(HeadlessNodeHealthCheck healthCheck, NineChroniclesClient graphqlClient)
    {
        _healthCheck = healthCheck;
        _graphqlClient = graphqlClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            bool completed;
            try
            {
                var tip = await _graphqlClient.Tip();
                completed = tip > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            _healthCheck.ConnectCompleted = completed;  
            await Task.Delay(3000, stoppingToken);
        }
    }
}
