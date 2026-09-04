using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentFlow.Domain.Entities;

namespace RentFlow.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.PropertyType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.City)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Amenities)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.City);

        builder.HasMany(x => x.Units)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}