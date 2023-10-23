using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class TransactionWorker : BackgroundService
{
    private readonly IDbContextFactory<RewardDbContext> _contextFactory;
    private readonly NineChroniclesClient _nineChroniclesClient;

    public TransactionWorker(NineChroniclesClient client, IDbContextFactory<RewardDbContext> contextFactory)
    {
        _nineChroniclesClient = client;
        _contextFactory = contextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested) stoppingToken.ThrowIfCancellationRequested();

            var dbContext = await _contextFactory.CreateDbContextAsync(stoppingToken);
            await StageTx(dbContext, _nineChroniclesClient, stoppingToken);
            await UpdateTx(dbContext, _nineChroniclesClient, stoppingToken);
            await Task.Delay(3000, stoppingToken);
        }
    }

    public static async Task UpdateTx(RewardDbContext dbContext, NineChroniclesClient client,
        CancellationToken stoppingToken)
    {
        var transactions = dbContext.Transactions
            .Where(p => p.Result == TransactionStatus.STAGING || p.Result == TransactionStatus.INVALID).ToList();
        foreach (var tx in transactions)
        {
            var result = await client.Result(tx.TxId);
            tx.Result = result.txStatus;
            tx.ExceptionName = result.exceptionName;
        }

        dbContext.UpdateRange(transactions);
        await dbContext.SaveChangesAsync(stoppingToken);
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
