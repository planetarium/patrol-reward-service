using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Converters;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class RewardDbContext : DbContext
{
    public RewardDbContext(DbContextOptions<RewardDbContext> options) : base(options)
    {
    }

    public DbSet<AvatarModel> Avatars { get; set; }
    public DbSet<RewardBaseModel> Rewards { get; set; }
    public DbSet<FungibleItemRewardModel> FungibleItemRewards { get; set; }
    public DbSet<FungibleAssetValueRewardModel> FungibleAssetValueRewards { get; set; }
    public DbSet<RewardPolicyModel> RewardPolicies { get; set; }
    public DbSet<ClaimModel> Claims { get; set; }
    public DbSet<GarageModel> Garages { get; set; }
    public DbSet<TransactionModel> Transactions { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Address>()
            .HaveConversion<AddressConverter>();
        configurationBuilder
            .Properties<TransactionStatus>()
            .HaveConversion<TransactionStatusConverter>();
        configurationBuilder
            .Properties<TxId>()
            .HaveConversion<TxIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<RewardBaseModel>()
            .HasDiscriminator<string>("reward_type")
            .HasValue<FungibleItemRewardModel>("item")
            .HasValue<FungibleAssetValueRewardModel>("fav");
        modelBuilder
            .Entity<AvatarModel>()
            .HasMany(p => p.Claims)
            .WithOne(p => p.Avatar)
            .HasForeignKey(p => p.AvatarAddress)
            .IsRequired();
        modelBuilder
            .Entity<AvatarModel>()
            .HasMany(p => p.TransactionModels)
            .WithOne(p => p.Avatar)
            .HasForeignKey(p => p.AvatarAddress)
            .IsRequired();
        modelBuilder
            .Entity<RewardPolicyModel>()
            .HasMany(e => e.Claims)
            .WithOne(e => e.Policy)
            .HasForeignKey(p => p.PolicyId)
            .IsRequired();
        modelBuilder
            .Entity<RewardPolicyModel>()
            .HasMany(e => e.Rewards)
            .WithMany(e => e.RewardPolicies);
        modelBuilder
            .Entity<TransactionModel>()
            .HasAlternateKey(p => p.Nonce);
        modelBuilder
            .Entity<TransactionModel>()
            .HasAlternateKey(p => new {p.AvatarAddress, p.ClaimCount});
        modelBuilder
            .Entity<ClaimModel>()
            .HasOne(e => e.Transaction)
            .WithOne(e => e.Claim)
            .HasForeignKey<ClaimModel>(e => e.TxId)
            .IsRequired();
    }
}
