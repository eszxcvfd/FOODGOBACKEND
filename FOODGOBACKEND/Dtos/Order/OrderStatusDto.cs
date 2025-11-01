namespace FOODGOBACKEND.Dtos.Order
{
    /// <summary>
    /// DTO for tracking order status.
    /// Customer Use Case C-UC07: Track order status.
    /// Provides real-time status updates for customer order tracking.
    /// </summary>
    public class OrderStatusDto
    {
        /// <summary>
        /// The unique identifier for the order.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// The unique order code displayed to users.
        /// </summary>
        public string OrderCode { get; set; } = null!;

        /// <summary>
        /// Current status of the order.
        /// Possible values: PENDING, CONFIRMED, PREPARED, DELIVERING, COMPLETED, CANCELLED
        /// </summary>
        public string OrderStatus { get; set; } = null!;

        /// <summary>
        /// Delivery address for the order.
        /// </summary>
        public string DeliveryAddress { get; set; } = null!;

        /// <summary>
        /// Total amount to be paid.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Status timeline tracking.
        /// </summary>
        public OrderStatusTimeline Timeline { get; set; } = null!;

        /// <summary>
        /// Restaurant basic information.
        /// </summary>
        public OrderStatusRestaurantInfo Restaurant { get; set; } = null!;

        /// <summary>
        /// Shipper basic information (if assigned).
        /// </summary>
        public OrderStatusShipperInfo? Shipper { get; set; }

        /// <summary>
        /// Estimated delivery time (if available).
        /// </summary>
        public DateTime? EstimatedDeliveryTime { get; set; }
    }

    /// <summary>
    /// Timeline of order status changes.
    /// </summary>
    public class OrderStatusTimeline
    {
        /// <summary>
        /// When the order was created.
        /// </summary>
        public DateTime? OrderPlaced { get; set; }

        /// <summary>
        /// When the restaurant confirmed the order.
        /// </summary>
        public DateTime? Confirmed { get; set; }

        /// <summary>
        /// When the restaurant finished preparing the order.
        /// </summary>
        public DateTime? Prepared { get; set; }

        /// <summary>
        /// When the shipper started delivering.
        /// </summary>
        public DateTime? OutForDelivery { get; set; }

        /// <summary>
        /// When the order was completed/delivered.
        /// </summary>
        public DateTime? Delivered { get; set; }

        /// <summary>
        /// When the order was cancelled (if applicable).
        /// </summary>
        public DateTime? Cancelled { get; set; }
    }

    /// <summary>
    /// Basic restaurant information for order tracking.
    /// </summary>
    public class OrderStatusRestaurantInfo
    {
        /// <summary>
        /// Restaurant ID.
        /// </summary>
        public int RestaurantId { get; set; }

        /// <summary>
        /// Restaurant name.
        /// </summary>
        public string RestaurantName { get; set; } = null!;

        /// <summary>
        /// Restaurant phone number for contact.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;
    }

    /// <summary>
    /// Basic shipper information for order tracking.
    /// </summary>
    public class OrderStatusShipperInfo
    {
        /// <summary>
        /// Shipper ID.
        /// </summary>
        public int ShipperId { get; set; }

        /// <summary>
        /// Shipper full name.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Shipper phone number for contact.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Vehicle license plate.
        /// </summary>
        public string? LicensePlate { get; set; }

        /// <summary>
        /// Current location latitude (for real-time tracking).
        /// </summary>
        public decimal? CurrentLat { get; set; }

        /// <summary>
        /// Current location longitude (for real-time tracking).
        /// </summary>
        public decimal? CurrentLng { get; set; }
    }
}