namespace FOODGOBACKEND.Dtos.Order
{
    /// <summary>
    /// DTO for displaying detailed information about an order.
    /// Used in:
    /// - Customer Use Case C-UC06: View order details
    /// - Restaurant Use Case R-UC03: View order details
    /// - Shipper Use Case S-UC03: View order details
    /// </summary>
    public class OrderDetailsDto
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
        /// Subtotal amount (sum of all items).
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Shipping/delivery fee.
        /// </summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Total amount to be paid (after discounts).
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// When the order was created.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// When the restaurant confirmed the order.
        /// </summary>
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>
        /// When the restaurant finished preparing the order.
        /// </summary>
        public DateTime? PreparedAt { get; set; }

        /// <summary>
        /// When the shipper started delivering.
        /// </summary>
        public DateTime? DeliveringAt { get; set; }

        /// <summary>
        /// When the order was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// When the order was cancelled (if applicable).
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// Information about the customer who placed the order.
        /// </summary>
        public OrderCustomerInfo Customer { get; set; } = null!;

        /// <summary>
        /// Information about the restaurant.
        /// </summary>
        public OrderRestaurantInfo Restaurant { get; set; } = null!;

        /// <summary>
        /// Information about the assigned shipper (if any).
        /// </summary>
        public OrderShipperInfo? Shipper { get; set; }

        /// <summary>
        /// List of items in the order.
        /// </summary>
        public List<OrderItemDetailsDto> Items { get; set; } = new List<OrderItemDetailsDto>();

        /// <summary>
        /// List of applied vouchers (if any).
        /// </summary>
        public List<OrderVoucherDetailsDto> AppliedVouchers { get; set; } = new List<OrderVoucherDetailsDto>();

        /// <summary>
        /// Payment information (if paid).
        /// </summary>
        public OrderPaymentInfo? Payment { get; set; }
    }

    /// <summary>
    /// Customer information embedded in order details.
    /// </summary>
    public class OrderCustomerInfo
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = null!;
    }

    /// <summary>
    /// Restaurant information embedded in order details.
    /// </summary>
    public class OrderRestaurantInfo
    {
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
    }

    /// <summary>
    /// Shipper information embedded in order details.
    /// </summary>
    public class OrderShipperInfo
    {
        public int ShipperId { get; set; }
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? LicensePlate { get; set; }
    }

    /// <summary>
    /// Detailed information about an item in the order.
    /// </summary>
    public class OrderItemDetailsDto
    {
        public int OrderItemId { get; set; }
        public int DishId { get; set; }
        public string DishName { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtOrder { get; set; }
        public decimal ItemTotal { get; set; } // Quantity * PriceAtOrder
    }

    /// <summary>
    /// Information about applied voucher.
    /// </summary>
    public class OrderVoucherDetailsDto
    {
        public string VoucherCode { get; set; } = null!;
        public string? Description { get; set; }
        public decimal DiscountApplied { get; set; }
    }

    /// <summary>
    /// Payment information for the order.
    /// </summary>
    public class OrderPaymentInfo
    {
        public int PaymentId { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public string? TransactionCode { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}