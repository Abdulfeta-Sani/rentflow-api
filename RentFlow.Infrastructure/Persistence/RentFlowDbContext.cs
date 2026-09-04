using Microsoft.EntityFrameworkCore;
using RentFlow.Domain.Entities;

namespace RentFlow.Infrastructure.Persistence;

public class RentFlowDbContext : DbContext
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<Tenancy> Tenancies => Set<Tenancy>();

    public RentFlowDbContext(DbContextOptions<RentFlowDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RentFlowDbContext).Assembly);
    }
}