using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using PatrolRewardService.Models;

namespace PatrolRewardService;

public class ServiceContext : DbContext
{
    public ServiceContext(DbContextOptions<ServiceContext> options) : base(options)
    {
    }

    public DbSet<PlayerModel> Players { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Address>()
            .HaveConversion<AddressConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<PlayerModel>()
            .Property(p => p.AvatarAddress)
            .HasConversion<AddressConverter>();
        modelBuilder
            .Entity<PlayerModel>()
            .Property(p => p.AgentAddress)
            .HasConversion<AddressConverter>();
    }
}
