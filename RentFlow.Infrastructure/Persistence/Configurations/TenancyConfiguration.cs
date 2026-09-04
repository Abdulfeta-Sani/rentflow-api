using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentFlow.Domain.Entities;

namespace RentFlow.Infrastructure.Persistence.Configurations;

public class TenancyConfiguration : IEntityTypeConfiguration<Tenancy>
{
    public void Configure(EntityTypeBuilder<Tenancy> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationId).IsRequired();
        builder.Property(x => x.UnitId).IsRequired();
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.OwnerId).IsRequired().HasMaxLength(100);

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();

        builder.Property(x => x.MonthlyRent)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.SecurityDeposit)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ActivatedAtUtc);
        builder.Property(x => x.EndedAtUtc);

        builder.HasIndex(x => x.UnitId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Application)
            .WithOne(x => x.Tenancy)
            .HasForeignKey<Tenancy>(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.Tenancies)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}