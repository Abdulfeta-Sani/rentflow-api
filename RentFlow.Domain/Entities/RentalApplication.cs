namespace RentFlow.Domain.Entities;

public class RentalApplication
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string EmploymentInformation { get; set; } = string.Empty;
    public int NumberOfOccupants { get; set; }
    public DateOnly PreferredMoveInDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public RentalApplicationStatus Status { get; set; } =
        RentalApplicationStatus.Submitted;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? DecisionReason { get; set; }
    public Unit Unit { get; set; } = null!;
    public Tenancy? Tenancy { get; set; }
}