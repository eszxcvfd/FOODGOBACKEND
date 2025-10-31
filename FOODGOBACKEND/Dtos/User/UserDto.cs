namespace FOODGOBACKEND.Dtos.User
{
    public class UserDto
    {
        public int UserId { get; set; }

        public string PhoneNumber { get; set; } = null!;

        public string UserType { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Flattened properties from related entities for convenience
        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? RestaurantName { get; set; }
    }
}