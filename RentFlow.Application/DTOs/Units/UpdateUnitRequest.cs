using System.ComponentModel.DataAnnotations;

namespace RentFlow.Application.DTOs.Units;

public sealed class UpdateUnitRequest : IValidatableObject
{
    [Required]
    [StringLength(80)]
    public string NameOrNumber { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Bedrooms { get; set; }

    [Range(0, int.MaxValue)]
    public int Bathrooms { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal MonthlyRent { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal SecurityDeposit { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NameOrNumber))
        {
            yield return new ValidationResult(
                "Unit name or number is required.",
                new[] { nameof(NameOrNumber) });
        }

        if (MonthlyRent <= 0)
        {
            yield return new ValidationResult(
                "Monthly rent must be greater than zero.",
                new[] { nameof(MonthlyRent) });
        }

        if (SecurityDeposit < 0)
        {
            yield return new ValidationResult(
                "Security deposit cannot be negative.",
                new[] { nameof(SecurityDeposit) });
        }

        if (Bedrooms < 0)
        {
            yield return new ValidationResult(
                "Bedrooms cannot be negative.",
                new[] { nameof(Bedrooms) });
        }

        if (Bathrooms < 0)
        {
            yield return new ValidationResult(
                "Bathrooms cannot be negative.",
                new[] { nameof(Bathrooms) });
        }
    }
}
