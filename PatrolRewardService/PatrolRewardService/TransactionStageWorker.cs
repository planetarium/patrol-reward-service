using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class TransactionStageWorker : BackgroundService
{
    private readonly IDbContextFactory<RewardDbContext> _contextFactory;
    private readonly NineChroniclesClient _nineChroniclesClient;
    private readonly int _interval;

    public TransactionStageWorker(NineChroniclesClient client, IDbContextFactory<RewardDbContext> contextFactory, IOptions<WorkerOptions> options)
    {
        _nineChroniclesClient = client;
        _contextFactory = contextFactory;
        _interval = options.Value.StageInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            var dbContext = await _contextFactory.CreateDbContextAsync(stoppingToken);
            await StageTx(dbContext, _nineChroniclesClient, stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    public static async Task StageTx(RewardDbContext dbContext, NineChroniclesClient client,
        CancellationToken stoppingToken)
    {
        var transactions = dbContext.Transactions
            .Where(p => p.Result == TransactionStatus.CREATED).ToList();
        foreach (var transaction in transactions)
        {
            var tx = Transaction.Deserialize(Convert.FromBase64String(transaction.Payload));
            await client.StageTx(tx);
            transaction.Result = TransactionStatus.STAGING;
        }

        dbContext.UpdateRange(transactions);
        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
