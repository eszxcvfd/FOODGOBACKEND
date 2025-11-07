using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Request DTO for updating user profile information.
    /// </summary>
    public class RequestUserProfileDto
    {
        /// <summary>
        /// The full name of the customer.
        /// </summary>
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name must not exceed 100 characters.")]
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The phone number of the customer.
        /// </summary>
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(15, ErrorMessage = "Phone number must not exceed 15 characters.")]
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// The email address of the customer.
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
        public string? Email { get; set; }

        /// <summary>
        /// The URL for the customer's avatar image.
        /// </summary>
        [StringLength(500, ErrorMessage = "Avatar URL must not exceed 500 characters.")]
        public string? AvatarUrl { get; set; }
    }
}