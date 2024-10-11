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
    private readonly ILogger<TransactionWorker> _logger;

    public TransactionWorker(NineChroniclesClient client, IDbContextFactory<RewardDbContext> contextFactory,IOptions<WorkerOptions> options, ILoggerFactory loggerFactory)
    {
        _nineChroniclesClient = client;
        _contextFactory = contextFactory;
        _interval = options.Value.ResultInterval;
        _logger = loggerFactory.CreateLogger<TransactionWorker>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var dbContext = await _contextFactory.CreateDbContextAsync(stoppingToken);
                await UpdateTx(dbContext, _nineChroniclesClient, stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (InvalidOperationException)
            {
                // pass
            }
            catch (Exception e)
            {
                // pass
                _logger.LogWarning(e, "worker raise error");
            }
        }
    }

    /// <summary>
    /// Update staged transactions result
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="client"></param>
    /// <param name="stoppingToken"></param>
    public static async Task UpdateTx(RewardDbContext dbContext, NineChroniclesClient client,
        CancellationToken stoppingToken)
    {
        var transactions = dbContext.Transactions
            .Where(p => p.Result == TransactionStatus.STAGING || p.Result == TransactionStatus.INVALID || p.Result == TransactionStatus.INCLUDED)
            .OrderBy(p => p.Nonce)
            .Take(100)
            .ToList();
        var txIds = transactions.Select(t => t.TxId.ToHex()).ToList();
        var results = await client.Results(txIds);
        var count = transactions.Count;
        for (int i = 0; i < count; i++)
        {
            var result = results[i];
            var tx = transactions[i];
            tx.Result = result.txStatus;
            tx.ExceptionName = result.exceptionNames?.FirstOrDefault();
        }

        dbContext.UpdateRange(transactions);
        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
