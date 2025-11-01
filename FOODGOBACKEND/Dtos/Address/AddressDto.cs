namespace FOODGOBACKEND.Dtos.Address
{
    /// <summary>
    /// DTO for displaying address information to the client.
    /// Used for viewing address lists and details.
    /// </summary>
    public class AddressDto
    {
        /// <summary>
        /// The unique identifier for the address.
        /// </summary>
        public int AddressId { get; set; }

        /// <summary>
        /// The customer ID who owns this address.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Street address.
        /// </summary>
        public string Street { get; set; } = null!;

        /// <summary>
        /// Ward/Commune (Phường/Xã).
        /// </summary>
        public string? Ward { get; set; }

        /// <summary>
        /// District (Quận/Huyện).
        /// </summary>
        public string? District { get; set; }

        /// <summary>
        /// City/Province (Thành phố/Tỉnh).
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Complete formatted address.
        /// </summary>
        public string FullAddress { get; set; } = null!;

        /// <summary>
        /// Indicates whether this is the default address for the customer.
        /// </summary>
        public bool IsDefault { get; set; }
    }
}