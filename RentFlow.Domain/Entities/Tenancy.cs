namespace RentFlow.Domain.Entities;

public class Tenancy
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid UnitId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public TenancyStatus Status { get; set; } = TenancyStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public RentalApplication Application { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
}