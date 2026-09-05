using System.ComponentModel.DataAnnotations;

namespace RentFlow.Application.DTOs.Units;

public sealed class UpdateUnitRequest
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
}
