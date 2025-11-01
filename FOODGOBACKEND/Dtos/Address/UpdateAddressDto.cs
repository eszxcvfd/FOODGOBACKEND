using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Address
{
    /// <summary>
    /// DTO for updating an existing address.
    /// All fields are optional to allow partial updates.
    /// </summary>
    public class UpdateAddressDto
    {
        /// <summary>
        /// Street address.
        /// </summary>
        [StringLength(200, ErrorMessage = "Street cannot exceed 200 characters.")]
        public string? Street { get; set; }

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
        [StringLength(500, ErrorMessage = "Full address cannot exceed 500 characters.")]
        public string? FullAddress { get; set; }

        /// <summary>
        /// Set or unset this address as the default address for the customer.
        /// </summary>
        public bool? IsDefault { get; set; }
    }
}