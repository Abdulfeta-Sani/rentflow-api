using System.ComponentModel.DataAnnotations;

namespace RentFlow.Application.DTOs.Properties;

public sealed class UpdatePropertyRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string PropertyType { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string City { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Amenities { get; set; } = string.Empty;
}
