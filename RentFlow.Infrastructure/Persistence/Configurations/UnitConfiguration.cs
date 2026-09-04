using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentFlow.Domain.Entities;

namespace RentFlow.Infrastructure.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PropertyId).IsRequired();
        builder.Property(x => x.NameOrNumber).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Bedrooms).IsRequired();
        builder.Property(x => x.Bathrooms).IsRequired();

        builder.Property(x => x.MonthlyRent)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.SecurityDeposit)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Applications)
            .WithOne(x => x.Unit)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Tenancies)
            .WithOne(x => x.Unit)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}