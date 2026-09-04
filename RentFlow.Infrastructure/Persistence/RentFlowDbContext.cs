using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentFlow.Domain.Entities;
using RentFlow.Infrastructure.Identity;

namespace RentFlow.Infrastructure.Persistence;

public class RentFlowDbContext : IdentityDbContext<AppUser>
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<Tenancy> Tenancies => Set<Tenancy>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public RentFlowDbContext(DbContextOptions<RentFlowDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.AccountStatus).HasConversion<string>();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
            entity.Property(x => x.TokenFamilyId).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);

            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.TokenFamilyId);

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RentFlowDbContext).Assembly);
    }
}