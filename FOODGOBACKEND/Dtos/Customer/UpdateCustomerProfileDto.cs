using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// DTO for updating customer profile information.
    /// All fields are optional to allow partial updates.
    /// </summary>
    public class UpdateCustomerProfileDto
    {
        /// <summary>
        /// The customer's full name.
        /// </summary>
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string? FullName { get; set; }

        /// <summary>
        /// The customer's email address.
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string? Email { get; set; }
    }
}