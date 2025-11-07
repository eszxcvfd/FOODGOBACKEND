namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Response DTO for detailed order information.
    /// </summary>
    public class ResponseOrderDetailDto
    {
        // ===== Status Section =====
        
        /// <summary>
        /// Status text displayed to user (e.g., "Đang tìm Tài xế...").
        /// </summary>
        public string StatusText { get; set; } = null!;

        /// <summary>
        /// Estimated delivery time text (e.g., "Dự kiến giao lúc 22:05 – 22:15").
        /// </summary>
        public string? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// Raw order status key (e.g., "PENDING", "DELIVERING", "COMPLETED").
        /// </summary>
        public string OrderStatusKey { get; set; } = null!;

        // ===== General Information =====
        
        /// <summary>
        /// Order code (e.g., "FDG-0001").
        /// </summary>
        public string OrderCode { get; set; } = null!;

        /// <summary>
        /// Customer note for the order.
        /// </summary>
        public string? Note { get; set; }

        // ===== Shipper Information =====
        
        /// <summary>
        /// Shipper information (null if no shipper assigned yet).
        /// </summary>
        public ShipperInfoDto? ShipperInfo { get; set; }

        // ===== Address Information =====
        
        /// <summary>
        /// Address information including restaurant and delivery details.
        /// </summary>
        public AddressInfoDto AddressInfo { get; set; } = null!;

        // ===== Order Summary (Financial) =====
        
        /// <summary>
        /// Financial summary of the order including items and pricing.
        /// </summary>
        public OrderSummaryDto Summary { get; set; } = null!;
    }
}