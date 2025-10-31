using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Auth
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "User type is required.")]
        [StringLength(20)]
        public string UserType { get; set; } = null!; // e.g., "Customer", "Shipper", "Restaurant"
    }
}