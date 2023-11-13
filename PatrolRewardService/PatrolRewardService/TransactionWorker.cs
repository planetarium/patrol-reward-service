using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class TransactionWorker : BackgroundService
{
    private readonly IDbContextFactory<RewardDbContext> _contextFactory;
    private readonly NineChroniclesClient _nineChroniclesClient;
    private readonly int _interval;

    public TransactionWorker(NineChroniclesClient client, IDbContextFactory<RewardDbContext> contextFactory,IOptions<WorkerOptions> options)
    {
        _nineChroniclesClient = client;
        _contextFactory = contextFactory;
        _interval = options.Value.ResultInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            var dbContext = await _contextFactory.CreateDbContextAsync(stoppingToken);
            await UpdateTx(dbContext, _nineChroniclesClient, stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    public static async Task UpdateTx(RewardDbContext dbContext, NineChroniclesClient client,
        CancellationToken stoppingToken)
    {
        var transactions = dbContext.Transactions
            .Where(p => p.Result == TransactionStatus.STAGING || p.Result == TransactionStatus.INVALID)
            .OrderBy(p => p.Nonce)
            .Take(100)
            .ToList();
        foreach (var tx in transactions)
        {
            var result = await client.Result(tx.TxId);
            tx.Result = result.txStatus;
            tx.ExceptionName = result.exceptionNames?.FirstOrDefault();
        }

        dbContext.UpdateRange(transactions);
        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
