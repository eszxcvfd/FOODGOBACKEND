namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Response DTO for user profile information.
    /// </summary>
    public class ResponseUserProfileDto
    {
        /// <summary>
        /// The full name of the customer.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The phone number of the customer.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// The email address of the customer.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// The URL for the customer's avatar image.
        /// </summary>
        public string? AvatarUrl { get; set; }
    }
}