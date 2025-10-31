using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Order
{
    /// <summary>
    /// DTO for updating the status of an order.
    /// </summary>
    public class UpdateOrderStatusDto
    {
        /// <summary>
        /// The new status for the order.
        /// </summary>
        [Required(ErrorMessage = "Order status is required.")]
        [StringLength(50)]
        public string OrderStatus { get; set; } = null!;
    }

    /// <summary>
    /// Constants for valid order statuses to ensure consistency.
    /// </summary>
    public static class OrderStatusConstants
    {
        public const string Pending = "PENDING";
        public const string Confirmed = "CONFIRMED";
        public const string Prepared = "PREPARED";
        public const string Delivering = "DELIVERING";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";

        /// <summary>
        /// Gets all valid order statuses.
        /// </summary>
        public static readonly string[] ValidStatuses = new[]
        {
            Pending,
            Confirmed,
            Prepared,
            Delivering,
            Completed,
            Cancelled
        };

        /// <summary>
        /// Statuses that a restaurant can set.
        /// </summary>
        public static readonly string[] RestaurantAllowedStatuses = new[]
        {
            Confirmed,
            Prepared,
            Cancelled
        };

        /// <summary>
        /// Statuses that a shipper can set.
        /// </summary>
        public static readonly string[] ShipperAllowedStatuses = new[]
        {
            Delivering,
            Completed
        };
    }
}