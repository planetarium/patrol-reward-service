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
    private readonly ILogger<TransactionStageWorker> _logger;
    private readonly int _stageTxCapacity;

    public TransactionStageWorker(NineChroniclesClient client, IDbContextFactory<RewardDbContext> contextFactory, IOptions<WorkerOptions> options, ILoggerFactory loggerFactory)
    {
        _nineChroniclesClient = client;
        _contextFactory = contextFactory;
        _interval = options.Value.StageInterval;
        _logger = loggerFactory.CreateLogger<TransactionStageWorker>();
        _stageTxCapacity = options.Value.StageTxCapacity;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var dbContext = await _contextFactory.CreateDbContextAsync(stoppingToken);
                var stagedTxCount = dbContext.Transactions.Count(t => t.Result == TransactionStatus.STAGING);
                if (stagedTxCount < _stageTxCapacity)
                {
                    await StageTx(dbContext, _nineChroniclesClient, stoppingToken);
                }
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
    /// Staging <see cref="TransactionStatus.CREATED"/> or <see cref="TransactionStatus.INVALID"/> transactions.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="client"></param>
    /// <param name="stoppingToken"></param>
    public static async Task StageTx(RewardDbContext dbContext, NineChroniclesClient client,
        CancellationToken stoppingToken)
    {
        var transactions = dbContext.Transactions
            .Where(p => p.Result == TransactionStatus.CREATED || p.Result == TransactionStatus.INVALID);
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
