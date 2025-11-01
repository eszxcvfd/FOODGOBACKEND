using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Shipper
{
    /// <summary>
    /// DTO for updating shipper profile information.
    /// All fields are optional to allow partial updates.
    /// </summary>
    public class UpdateShipperProfileDto
    {
        /// <summary>
        /// The shipper's full name.
        /// </summary>
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string? FullName { get; set; }

        /// <summary>
        /// Vehicle license plate number.
        /// </summary>
        [StringLength(20, ErrorMessage = "License plate cannot exceed 20 characters.")]
        public string? LicensePlate { get; set; }

        /// <summary>
        /// Indicates whether the shipper is currently available for deliveries.
        /// </summary>
        public bool? IsAvailable { get; set; }

        /// <summary>
        /// Current latitude location of the shipper.
        /// Used for real-time location tracking.
        /// </summary>
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal? CurrentLat { get; set; }

        /// <summary>
        /// Current longitude location of the shipper.
        /// Used for real-time location tracking.
        /// </summary>
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal? CurrentLng { get; set; }
    }
}