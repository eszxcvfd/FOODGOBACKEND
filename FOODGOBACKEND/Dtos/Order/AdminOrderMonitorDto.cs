namespace FOODGOBACKEND.Dtos.Order
{
    /// <summary>
    /// DTO for admin to monitor and manage all orders in the system.
    /// Admin Use Case A-UC04: Monitor all orders with comprehensive information.
    /// Provides a complete overview for administrative purposes.
    /// </summary>
    public class AdminOrderMonitorDto
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
        /// </summary>
        public string OrderStatus { get; set; } = null!;

        /// <summary>
        /// Delivery address for the order.
        /// </summary>
        public string DeliveryAddress { get; set; } = null!;

        /// <summary>
        /// Optional note/instructions for the order.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Financial information.
        /// </summary>
        public AdminOrderFinancialInfo Financial { get; set; } = null!;

        /// <summary>
        /// Complete timeline of the order.
        /// </summary>
        public AdminOrderTimeline Timeline { get; set; } = null!;

        /// <summary>
        /// Duration statistics (for performance monitoring).
        /// </summary>
        public AdminOrderDurationStats DurationStats { get; set; } = null!;

        /// <summary>
        /// Customer information.
        /// </summary>
        public AdminOrderCustomerInfo Customer { get; set; } = null!;

        /// <summary>
        /// Restaurant information.
        /// </summary>
        public AdminOrderRestaurantInfo Restaurant { get; set; } = null!;

        /// <summary>
        /// Shipper information (if assigned).
        /// </summary>
        public AdminOrderShipperInfo? Shipper { get; set; }

        /// <summary>
        /// Order items summary.
        /// </summary>
        public AdminOrderItemsSummary ItemsSummary { get; set; } = null!;

        /// <summary>
        /// List of items in the order.
        /// </summary>
        public List<AdminOrderItemInfo> Items { get; set; } = new List<AdminOrderItemInfo>();

        /// <summary>
        /// Vouchers and discounts applied.
        /// </summary>
        public List<AdminOrderVoucherInfo> AppliedVouchers { get; set; } = new List<AdminOrderVoucherInfo>();

        /// <summary>
        /// Payment information.
        /// </summary>
        public AdminOrderPaymentInfo? Payment { get; set; }

        /// <summary>
        /// Indicates if the order has any issues or requires attention.
        /// </summary>
        public bool RequiresAttention { get; set; }

        /// <summary>
        /// List of potential issues detected.
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Financial breakdown for admin monitoring.
    /// </summary>
    public class AdminOrderFinancialInfo
    {
        /// <summary>
        /// Subtotal (sum of all items before discounts).
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Total discount applied from vouchers.
        /// </summary>
        public decimal TotalDiscount { get; set; }

        /// <summary>
        /// Shipping/delivery fee.
        /// </summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Platform commission (estimated percentage).
        /// </summary>
        public decimal PlatformCommission { get; set; }

        /// <summary>
        /// Restaurant's earning (after commission).
        /// </summary>
        public decimal RestaurantEarning { get; set; }

        /// <summary>
        /// Shipper's earning.
        /// </summary>
        public decimal ShipperEarning { get; set; }

        /// <summary>
        /// Total amount paid by customer.
        /// </summary>
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// Complete timeline with all timestamps.
    /// </summary>
    public class AdminOrderTimeline
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
        /// When the restaurant finished preparing.
        /// </summary>
        public DateTime? Prepared { get; set; }

        /// <summary>
        /// When the shipper accepted the order.
        /// </summary>
        public DateTime? ShipperAssigned { get; set; }

        /// <summary>
        /// When the shipper started delivering.
        /// </summary>
        public DateTime? OutForDelivery { get; set; }

        /// <summary>
        /// When the order was delivered/completed.
        /// </summary>
        public DateTime? Delivered { get; set; }

        /// <summary>
        /// When the order was cancelled (if applicable).
        /// </summary>
        public DateTime? Cancelled { get; set; }
    }

    /// <summary>
    /// Duration statistics for performance monitoring.
    /// </summary>
    public class AdminOrderDurationStats
    {
        /// <summary>
        /// Time taken for restaurant to confirm (in minutes).
        /// </summary>
        public int? TimeToConfirm { get; set; }

        /// <summary>
        /// Time taken for restaurant to prepare (in minutes).
        /// </summary>
        public int? TimeToPrepare { get; set; }

        /// <summary>
        /// Time taken for shipper to accept (in minutes).
        /// </summary>
        public int? TimeToAssignShipper { get; set; }

        /// <summary>
        /// Time taken for delivery (in minutes).
        /// </summary>
        public int? TimeToDeliver { get; set; }

        /// <summary>
        /// Total time from order placed to delivered (in minutes).
        /// </summary>
        public int? TotalDuration { get; set; }
    }

    /// <summary>
    /// Customer information for admin view.
    /// </summary>
    public class AdminOrderCustomerInfo
    {
        /// <summary>
        /// Customer ID.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Customer full name.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Customer email.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Customer phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Total number of orders placed by this customer.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Customer account status.
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Restaurant information for admin view.
    /// </summary>
    public class AdminOrderRestaurantInfo
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
        /// Restaurant address.
        /// </summary>
        public string Address { get; set; } = null!;

        /// <summary>
        /// Restaurant phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Restaurant account status.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Average rating of the restaurant.
        /// </summary>
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Total completed orders from this restaurant.
        /// </summary>
        public int TotalCompletedOrders { get; set; }
    }

    /// <summary>
    /// Shipper information for admin view.
    /// </summary>
    public class AdminOrderShipperInfo
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
        /// Shipper phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Vehicle license plate.
        /// </summary>
        public string? LicensePlate { get; set; }

        /// <summary>
        /// Current availability status.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Current location latitude.
        /// </summary>
        public decimal? CurrentLat { get; set; }

        /// <summary>
        /// Current location longitude.
        /// </summary>
        public decimal? CurrentLng { get; set; }

        /// <summary>
        /// Total completed deliveries by this shipper.
        /// </summary>
        public int TotalDeliveries { get; set; }
    }

    /// <summary>
    /// Order items summary for quick overview.
    /// </summary>
    public class AdminOrderItemsSummary
    {
        /// <summary>
        /// Total number of different items.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total quantity of all items.
        /// </summary>
        public int TotalQuantity { get; set; }

        /// <summary>
        /// Most expensive item in the order.
        /// </summary>
        public string? MostExpensiveItem { get; set; }
    }

    /// <summary>
    /// Detailed item information for admin.
    /// </summary>
    public class AdminOrderItemInfo
    {
        /// <summary>
        /// Order item ID.
        /// </summary>
        public int OrderItemId { get; set; }

        /// <summary>
        /// Dish ID.
        /// </summary>
        public int DishId { get; set; }

        /// <summary>
        /// Dish name.
        /// </summary>
        public string DishName { get; set; } = null!;

        /// <summary>
        /// Quantity ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Price at the time of order.
        /// </summary>
        public decimal PriceAtOrder { get; set; }

        /// <summary>
        /// Current price of the dish (for comparison).
        /// </summary>
        public decimal? CurrentPrice { get; set; }

        /// <summary>
        /// Item total (Quantity * PriceAtOrder).
        /// </summary>
        public decimal ItemTotal { get; set; }

        /// <summary>
        /// Whether the dish is still available.
        /// </summary>
        public bool IsStillAvailable { get; set; }
    }

    /// <summary>
    /// Voucher information for admin view.
    /// </summary>
    public class AdminOrderVoucherInfo
    {
        /// <summary>
        /// Voucher ID.
        /// </summary>
        public int VoucherId { get; set; }

        /// <summary>
        /// Voucher code.
        /// </summary>
        public string VoucherCode { get; set; } = null!;

        /// <summary>
        /// Voucher description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Discount type (Percentage or FixedAmount).
        /// </summary>
        public string DiscountType { get; set; } = null!;

        /// <summary>
        /// Discount value.
        /// </summary>
        public decimal DiscountValue { get; set; }

        /// <summary>
        /// Actual discount applied to this order.
        /// </summary>
        public decimal DiscountApplied { get; set; }

        /// <summary>
        /// Whether the voucher is still active.
        /// </summary>
        public bool IsStillActive { get; set; }
    }

    /// <summary>
    /// Payment information for admin view.
    /// </summary>
    public class AdminOrderPaymentInfo
    {
        /// <summary>
        /// Payment ID.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Payment method.
        /// </summary>
        public string PaymentMethod { get; set; } = null!;

        /// <summary>
        /// Payment amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment status.
        /// </summary>
        public string PaymentStatus { get; set; } = null!;

        /// <summary>
        /// Transaction code.
        /// </summary>
        public string? TransactionCode { get; set; }

        /// <summary>
        /// When the payment was created.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Whether payment matches order total.
        /// </summary>
        public bool IsAmountCorrect { get; set; }
    }
}