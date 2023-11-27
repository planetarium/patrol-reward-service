using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PatrolRewardService.GraphqlTypes;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class ContextService : IAsyncDisposable, IDisposable
{
    private bool _disposed;
    private readonly RewardDbContext _dbContext;
    private readonly IDbContextFactory<RewardDbContext> _dbContextFactory;
    private readonly IConfiguration _configuration;

    public ContextService(IDbContextFactory<RewardDbContext> dbContextFactory, IConfiguration configuration)
    {
        _dbContext = dbContextFactory.CreateDbContext();
        _dbContextFactory = dbContextFactory;
        _configuration = configuration;
    }

    public RewardDbContext CreateDbContext()
    {
        return _dbContextFactory.CreateDbContext();
    }

    public async Task<RewardDbContext> CreateDbContextAsync()
    {
        return await _dbContextFactory.CreateDbContextAsync();
    }

    public AvatarModel? GetAvatar(string avatarAddress, string agentAddress, bool track = false)
    {
        IQueryable<AvatarModel> baseQuery = _dbContext.Avatars;
        if (!track)
        {
            baseQuery = baseQuery.AsNoTracking();
        }
        return baseQuery.FirstOrDefault(p =>
            p.AvatarAddress == new Address(avatarAddress) && p.AgentAddress == new Address(agentAddress));
    }

    public RewardPolicyModel GetPolicy(bool free, int level)
    {
        var policies = _dbContext
            .RewardPolicies
            .Include(p => p.Rewards)
            .Where(p => p.Activate && p.Free == free)
            .OrderByDescending(p => p.MinimumLevel)
            .ToList();
        foreach (var policy in policies)
            if (policy.MinimumLevel <= level)
                return policy;

        return policies.Last();
    }

    public RewardBaseModel? GetReward(RewardInput rewardInput)
    {
        if (rewardInput.IsItemReward())
        {
            return _dbContext
                .FungibleItemRewards
                .Include(r => r.RewardPolicies)
                .FirstOrDefault(p =>
                    p.RewardInterval == rewardInput.RewardInterval
                    && p.PerInterval == rewardInput.PerInterval
                    && p.FungibleId == rewardInput.FungibleId
                    && p.ItemId == rewardInput.ItemId
                );
        }

        return _dbContext
            .FungibleAssetValueRewards
            .Include(r => r.RewardPolicies)
            .FirstOrDefault(p =>
                p.RewardInterval == rewardInput.RewardInterval
                && p.PerInterval == rewardInput.PerInterval
                && p.Currency == rewardInput.Currency
                && p.Ticker == rewardInput.Ticker
            );
    }

    public async Task<int> PutClaimPolicy(List<RewardBaseModel> rewards, bool free, TimeSpan interval, bool activate,
        int minimumLevel, string password, int? maxLevel = null)
    {
        if (password != _configuration["PatrolReward:ApiKey"])
        {
            throw new UnauthorizedAccessException();
        }
        if (rewards.Any(r => r.RewardInterval != interval))
        {
            throw new ArgumentException("reward interval must be equal to policy interval.");
        }
        var policy = await _dbContext
            .RewardPolicies
            .Include(r => r.Rewards)
            .FirstOrDefaultAsync(r => r.Free == free && r.Activate == activate && r.MinimumLevel == minimumLevel);
        if (policy is null)
        {
            policy = new RewardPolicyModel
            {
                Rewards = rewards,
                Free = free,
                MinimumRequiredInterval = interval,
                Activate = activate,
                MinimumLevel = minimumLevel,
                MaxLevel = maxLevel,
            };
            await _dbContext.RewardPolicies.AddAsync(policy);
        }
        else
        {
            policy.Rewards.Clear();
            policy.Rewards = rewards;
            policy.MaxLevel = maxLevel;
            policy.MinimumRequiredInterval = interval;
            _dbContext.RewardPolicies.Update(policy);
        }

        await _dbContext.SaveChangesAsync();
        return policy.Id;
    }

    public async Task<TransactionModel?> GetTransaction(AvatarModel avatar)
    {
        return await _dbContext
            .Transactions
            .FirstOrDefaultAsync(t => t.AvatarAddress == avatar.AvatarAddress && t.ClaimCount == avatar.ClaimCount);
    }

    public async Task<TransactionModel?> GetTransaction(TxId txId)
    {
        return await _dbContext
            .Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TxId == txId);
    }

    public async Task<long> GetNonce()
    {
        long nonce = 0L;
        try
        {
            nonce = await _dbContext.Transactions.Select(p => p.Nonce).MaxAsync() + 1;
        }
        catch (InvalidOperationException)
        {
            //pass
        }

        return nonce;
    }

    public async Task InsertTransaction(TransactionModel transaction)
    {
        await _dbContext.Transactions.AddAsync(transaction);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<AvatarModel> PutAvatar(NineChroniclesClient.Avatar avatarState, string avatarAddress, string agentAddress, bool save = true)
    {
        var avatarExist = true;
        var avatar = GetAvatar(avatarAddress, agentAddress, true);
        if (avatar is null)
        {
            avatarExist = false;
            avatar = new AvatarModel
            {
                AvatarAddress = new Address(avatarAddress),
                AgentAddress = new Address(agentAddress),
                CreatedAt = DateTime.UtcNow
            };
            var policy = GetPolicy(true, avatarState.Level);
            avatar.LastClaimedAt = avatar.CreatedAt - policy.MinimumRequiredInterval;
        }
    
        avatar.Level = avatarState.Level;
        if (save)
        {
            if (avatarExist)
                _dbContext.Avatars.Update(avatar);
            else
                _dbContext.Avatars.Add(avatar);
            await _dbContext.SaveChangesAsync();
        }
    
        return avatar;
    }

    public IQueryable<TransactionModel> Transactions()
    {
        return _dbContext.Transactions;
    }

    public async Task<TxId?> RetryTransaction(Signer signer, NineChroniclesClient client, TxId txId, string password)
    {
        if (password != _configuration["PatrolReward:ApiKey"])
        {
            throw new UnauthorizedAccessException();
        }

        var transaction = await _dbContext
            .Transactions
            .Include(t => t.Avatar)
            .Include(t => t.Claim)
            .ThenInclude(c => c.Garages)
            .ThenInclude(g => g.Reward)
            .FirstOrDefaultAsync(t => t.TxId == txId);
        if (transaction is null)
        {
            return null;
        }

        return await RetryTx(signer, transaction);
    }

    private async Task<TxId> RetryTx(Signer signer, TransactionModel transaction)
    {
        var newNonce = await GetNonce();
        var avatar = transaction.Avatar;
        var memo = $"retry patrol reward {avatar.AvatarAddress} / {avatar.ClaimCount}";
        var action = transaction.Claim.ToClaimItems(avatar.AvatarAddress, avatar.AgentAddress, memo);
        var now = DateTime.UtcNow;
        var tx = signer.Sign(newNonce, new[] {action}, 1 * Currencies.Mead, 4L, now + TimeSpan.FromDays(1));
        var newTransaction = new TransactionModel
        {
            Avatar = avatar,
            CreatedAt = now,
            ClaimCount = avatar.ClaimCount,
            Nonce = newNonce,
            TxId = tx.Id,
            Payload = Convert.ToBase64String(tx.Serialize()),
            Claim = transaction.Claim,
            GasLimit = tx.GasLimit,
            Gas = 1
        };
        await _dbContext.Database.BeginTransactionAsync();
        await InsertTransaction(newTransaction);
        await _dbContext.Database.ExecuteSqlRawAsync(
            $"UPDATE transactions set result = '{TransactionStatus.FAILURE}', exception_name = '{memo}' where tx_id = '{transaction.TxId}'");
        await _dbContext.Database.CommitTransactionAsync();

        return tx.Id;
    }

    public async Task<List<TxId>> RetryTransactions(Signer signer, NineChroniclesClient client, int startNonce, int endNonce, string password)
    {
        if (password != _configuration["PatrolReward:ApiKey"])
        {
            throw new UnauthorizedAccessException();
        }

        var transactions = await _dbContext
            .Transactions
            .Include(t => t.Avatar)
            .Include(t => t.Claim)
            .ThenInclude(c => c.Garages)
            .ThenInclude(g => g.Reward)
            .Where(t => t.Result == TransactionStatus.FAILURE && t.Nonce >= startNonce && t.Nonce <= endNonce)
            .ToListAsync();
        var result = new List<TxId>();
        if (!transactions.Any())
        {
            return result;
        }

        foreach (var transaction in transactions)
        {
            TxId txId = await RetryTx(signer, transaction);
            result.Add(txId);
        }

        return result;
    }

    public async Task<List<TxId>> ReplaceTransactions(Signer signer, NineChroniclesClient client, int startNonce, int endNonce, string password)
    {
        if (password != _configuration["PatrolReward:ApiKey"])
        {
            throw new UnauthorizedAccessException();
        }

        var transactions = await _dbContext
            .Transactions
            .Include(t => t.Avatar)
            .Include(t => t.Claim)
            .ThenInclude(c => c.Garages)
            .ThenInclude(g => g.Reward)
            .Where(t => t.Result == TransactionStatus.INVALID && t.Nonce >= startNonce && t.Nonce <= endNonce)
            .ToListAsync();
        var result = new List<TxId>();
        if (!transactions.Any())
        {
            return result;
        }

        foreach (var transaction in transactions)
        {
            var avatar = transaction.Avatar;
            var memo = $"replace patrol reward {avatar.AvatarAddress} / {transaction.ClaimCount} / {transaction.Nonce} / {transaction.TxId}";
            var action = transaction.Claim.ToClaimItems(avatar.AvatarAddress, avatar.AgentAddress, memo);
            var now = DateTime.UtcNow;
            var tx = signer.Sign(transaction.Nonce, new[] {action}, 1 * Currencies.Mead, 4L, now + TimeSpan.FromDays(1));
            var txId = tx.Id;
            var payload = Convert.ToBase64String(tx.Serialize());
            await _dbContext.Database.BeginTransactionAsync();
            var param = new NpgsqlParameter("@now", now);
            await _dbContext.Database.ExecuteSqlRawAsync(
                $"UPDATE transactions set tx_id = '{txId}', created_at = @now, payload = '{payload}', result = '{TransactionStatus.CREATED}' where nonce = {transaction.Nonce}", param);
            await _dbContext.Database.CommitTransactionAsync();
            result.Add(tx.Id);
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _dbContext.Dispose();
            }

            _disposed = true;
        }
    }    
    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _dbContext.DisposeAsync();
    }

    public async Task<List<TxId>> StageTransactions(NineChroniclesClient client, int startNonce, int endNonce, string password)
    {
        if (password != _configuration["PatrolReward:ApiKey"])
        {
            throw new UnauthorizedAccessException();
        }

        var transactions = await _dbContext
            .Transactions
            .Where(t => t.Result == TransactionStatus.INVALID && t.Nonce >= startNonce && t.Nonce <= endNonce)
            .ToListAsync();
        var result = new List<TxId>();
        if (!transactions.Any())
        {
            return result;
        }

        foreach (var transaction in transactions)
        {
            var tx = Transaction.Deserialize(Convert.FromBase64String(transaction.Payload));
            var txId = tx.Id;
            await client.StageTx(tx);
            result.Add(tx.Id);
        }

        return result;
    }

    public async Task<int> InvalidTxCount()
    {
        return await _dbContext.Transactions.CountAsync(t => t.Result == TransactionStatus.INVALID);
    }
}
