namespace FOODGOBACKEND.Dtos.Shipper
{
    /// <summary>
    /// DTO for displaying shipper profile information.
    /// Used when a shipper views or retrieves their own profile.
    /// </summary>
    public class ShipperProfileDto
    {
        /// <summary>
        /// The unique identifier for the shipper.
        /// </summary>
        public int ShipperId { get; set; }

        /// <summary>
        /// The shipper's full name.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The shipper's phone number (from User entity).
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Vehicle license plate number.
        /// </summary>
        public string? LicensePlate { get; set; }

        /// <summary>
        /// Indicates whether the shipper is currently available for deliveries.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Current latitude location of the shipper.
        /// </summary>
        public decimal? CurrentLat { get; set; }

        /// <summary>
        /// Current longitude location of the shipper.
        /// </summary>
        public decimal? CurrentLng { get; set; }

        /// <summary>
        /// Indicates whether the shipper account is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When the shipper account was created.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the shipper account was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Total number of orders delivered by the shipper.
        /// </summary>
        public int TotalDeliveries { get; set; }

        /// <summary>
        /// Total number of orders currently in progress (delivering).
        /// </summary>
        public int ActiveDeliveries { get; set; }

        /// <summary>
        /// Total number of completed deliveries.
        /// </summary>
        public int CompletedDeliveries { get; set; }
    }
}