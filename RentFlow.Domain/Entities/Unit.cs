namespace RentFlow.Domain.Entities;

public class Unit
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string NameOrNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Available;
    public Property Property { get; set; } = null!;
    public ICollection<RentalApplication> Applications { get; set; } =
        new List<RentalApplication>();
    public ICollection<Tenancy> Tenancies { get; set; } =
        new List<Tenancy>();
}