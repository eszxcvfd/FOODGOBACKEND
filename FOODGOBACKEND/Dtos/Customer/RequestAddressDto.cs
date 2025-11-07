using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// DTO for creating a new address for a customer.
    /// </summary>
    public class RequestAddressDto
    {
        /// <summary>
        /// Street address.
        /// </summary>
        [Required(ErrorMessage = "Street is required.")]
        [StringLength(255, ErrorMessage = "Street must not exceed 255 characters.")]
        public string Street { get; set; } = null!;

        /// <summary>
        /// Ward (optional).
        /// </summary>
        [StringLength(100, ErrorMessage = "Ward must not exceed 100 characters.")]
        public string? Ward { get; set; }

        /// <summary>
        /// District (optional).
        /// </summary>
        [StringLength(100, ErrorMessage = "District must not exceed 100 characters.")]
        public string? District { get; set; }

        /// <summary>
        /// City (optional).
        /// </summary>
        [StringLength(100, ErrorMessage = "City must not exceed 100 characters.")]
        public string? City { get; set; }

        /// <summary>
        /// Full address (will be auto-generated if not provided).
        /// </summary>
        [StringLength(500, ErrorMessage = "Full address must not exceed 500 characters.")]
        public string? FullAddress { get; set; }

        /// <summary>
        /// Set this address as default (optional, default: false).
        /// </summary>
        public bool IsDefault { get; set; } = false;
    }
}