using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentFlow.Domain.Entities;

namespace RentFlow.Infrastructure.Persistence.Configurations;

public class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.UnitId)
            .IsRequired();

        builder.Property(x => x.EmploymentInformation)
            .HasMaxLength(2000);

        builder.Property(x => x.NumberOfOccupants)
            .IsRequired();

        builder.Property(x => x.PreferredMoveInDate)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SubmittedAtUtc)
            .IsRequired();

        builder.Property(x => x.ReviewedAtUtc);

        builder.Property(x => x.DecisionReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UnitId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Unit)
            .WithMany(x => x.Applications)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tenancy)
            .WithOne(x => x.Application)
            .HasForeignKey<Tenancy>(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}