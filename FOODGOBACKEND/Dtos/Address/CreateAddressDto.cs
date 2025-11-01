using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Address
{
    /// <summary>
    /// DTO for creating a new address.
    /// Used when a customer adds a new delivery address.
    /// </summary>
    public class CreateAddressDto
    {
        /// <summary>
        /// Street address.
        /// </summary>
        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200, ErrorMessage = "Street cannot exceed 200 characters.")]
        public string Street { get; set; } = null!;

        /// <summary>
        /// Ward/Commune (Phường/Xã).
        /// </summary>
        [StringLength(100, ErrorMessage = "Ward cannot exceed 100 characters.")]
        public string? Ward { get; set; }

        /// <summary>
        /// District (Quận/Huyện).
        /// </summary>
        [StringLength(100, ErrorMessage = "District cannot exceed 100 characters.")]
        public string? District { get; set; }

        /// <summary>
        /// City/Province (Thành phố/Tỉnh).
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        /// <summary>
        /// Complete formatted address.
        /// </summary>
        [Required(ErrorMessage = "Full address is required.")]
        [StringLength(500, ErrorMessage = "Full address cannot exceed 500 characters.")]
        public string FullAddress { get; set; } = null!;

        /// <summary>
        /// Set this address as the default address for the customer.
        /// </summary>
        public bool IsDefault { get; set; } = false;
    }
}