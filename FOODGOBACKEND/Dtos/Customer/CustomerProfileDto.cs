namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// DTO for displaying customer profile information.
    /// Used when a customer views or retrieves their own profile.
    /// </summary>
    public class CustomerProfileDto
    {
        /// <summary>
        /// The unique identifier for the customer.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// The customer's full name.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The customer's email address.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// The customer's phone number (from User entity).
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Indicates whether the customer account is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When the customer account was created.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the customer account was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Total number of orders placed by the customer.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Total number of reviews written by the customer.
        /// </summary>
        public int TotalReviews { get; set; }

        /// <summary>
        /// Total number of saved addresses.
        /// </summary>
        public int TotalAddresses { get; set; }
    }
}