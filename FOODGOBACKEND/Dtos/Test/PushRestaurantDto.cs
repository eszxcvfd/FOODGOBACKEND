namespace FOODGOBACKEND.Dtos.Test
{
    /// <summary>
    /// DTO for creating/pushing a new restaurant (testing purpose).
    /// </summary>
    public class PushRestaurantDto
    {
        /// <summary>
        /// Owner's User ID (must be RESTAURANT_OWNER role).
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// Restaurant name.
        /// </summary>
        public string RestaurantName { get; set; } = null!;

        /// <summary>
        /// Restaurant address.
        /// </summary>
        public string Address { get; set; } = null!;

        /// <summary>
        /// Restaurant phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Opening time (HH:mm format).
        /// </summary>
        public string? OpeningTime { get; set; }

        /// <summary>
        /// Closing time (HH:mm format).
        /// </summary>
        public string? ClosingTime { get; set; }

        /// <summary>
        /// Is the restaurant active? Default: true.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}