namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents a customer's address information.
    /// </summary>
    public class ItemAddressDto
    {
        /// <summary>
        /// The unique identifier for the address.
        /// </summary>
        public int AddressId { get; set; }

        /// <summary>
        /// The name of the customer associated with the address.
        /// </summary>
        public string CustomerName { get; set; } = null!;

        /// <summary>
        /// The phone number of the customer.
        /// </summary>
        public string CustomerPhone { get; set; } = null!;

        /// <summary>
        /// The full delivery address.
        /// </summary>
        public string FullAddress { get; set; } = null!;

        /// <summary>
        /// Indicates if this is the default address for the customer.
        /// </summary>
        public bool IsDefault { get; set; }
    }
}