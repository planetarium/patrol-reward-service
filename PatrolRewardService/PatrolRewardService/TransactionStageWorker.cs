using System.Diagnostics;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
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
    private readonly Signer _signer;

    public TransactionStageWorker(NineChroniclesClient client, IDbContextFactory<RewardDbContext> contextFactory, IOptions<WorkerOptions> options, ILoggerFactory loggerFactory, Signer signer)
    {
        _nineChroniclesClient = client;
        _contextFactory = contextFactory;
        _interval = options.Value.StageInterval;
        _logger = loggerFactory.CreateLogger<TransactionStageWorker>();
        _stageTxCapacity = options.Value.StageTxCapacity;
        _signer = signer;
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
                    await Odin(dbContext, _nineChroniclesClient, stoppingToken);
                    // await StageTx(dbContext, _nineChroniclesClient, stoppingToken);
                }
                // await Task.Delay(_interval, stoppingToken);
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
            .Where(p => p.Result == TransactionStatus.CREATED || p.Result == TransactionStatus.INVALID)
            .OrderBy(p => p.Nonce)
            .Take(100);
        foreach (var transaction in transactions)
        {
            var tx = Transaction.Deserialize(Convert.FromBase64String(transaction.Payload));
            await client.StageTx(tx);
            transaction.Result = TransactionStatus.STAGING;
        }

        dbContext.UpdateRange(transactions);
        await dbContext.SaveChangesAsync(stoppingToken);
    }

    public async Task Odin(RewardDbContext dbContext, NineChroniclesClient client,
        CancellationToken stoppingToken)
    {
        var ts = DateTime.UtcNow - TimeSpan.FromHours(1);
        var startNonce = await client.Nonce();
        var transactions = await dbContext
            .Transactions
            .Include(t => t.Avatar)
            .Include(t => t.Claim)
            .ThenInclude(c => c.Garages)
            .ThenInclude(g => g.Reward)
            .Where(t => (t.Result == TransactionStatus.CREATED || t.Result == TransactionStatus.INVALID || t.Result == TransactionStatus.STAGING || t.Result == TransactionStatus.INCLUDED) && t.CreatedAt < ts && t.Nonce >= startNonce)
            .OrderBy(t => t.Nonce)
            .Take(100)
            .ToListAsync(stoppingToken);
        if (transactions.Any())
        {
            var txIds = new List<string>();
            foreach (var transaction in transactions)
            {
                var avatar = transaction.Avatar;
                var memo =
                    $"replace patrol reward {avatar.AvatarAddress} / {transaction.ClaimCount} / {transaction.Nonce} / {transaction.TxId}";
                _logger.LogInformation($"startNonce: {startNonce}, memo: {memo}, targetNonce: {transaction.Nonce}");
                var action = transaction.Claim.ToClaimItems(avatar.AvatarAddress, avatar.AgentAddress, memo);
                var now = DateTime.UtcNow;
                var tx = _signer.Sign(transaction.Nonce, new[] {action}, 1 * Currencies.Mead, 4L,
                    now + TimeSpan.FromDays(1));
                var txId = tx.Id;
                var payload = Convert.ToBase64String(tx.Serialize());
                Debug.Assert(tx.Signer.Equals(new Address("0xCaD60f18b4Ba189f7f1c14E2267D9b20F5b16Ff5")));
                await dbContext.Database.BeginTransactionAsync();
                var param = new NpgsqlParameter("@now", now);
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"UPDATE transactions set tx_id = '{txId}', created_at = @now, payload = '{payload}', result = '{TransactionStatus.STAGING}' where nonce = {transaction.Nonce}", param);
                await dbContext.Database.CommitTransactionAsync(stoppingToken);
                await client.StageTx(tx);
                txIds.Add(txId.ToHex());
            }
            await Task.Delay(_interval, stoppingToken);
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
}
