namespace FOODGOBACKEND.Dtos.Restaurant
{
    /// <summary>
    /// DTO for displaying order information in restaurant's order list.
    /// </summary>
    public class ItemOrderDto
    {
        /// <summary>
        /// The unique identifier for the order.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Order code (e.g., "19042-381197523").
        /// Maps to @id/tvOrderCode in the UI.
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Name of the customer who placed the order.
        /// Maps to @id/tvCustomerName in the UI.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Number of items in the order.
        /// Maps to @id/tvItemCount in the UI.
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Distance from restaurant to delivery address in kilometers.
        /// Maps to @id/tvDistance in the UI.
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Total price of the order.
        /// Maps to @id/tvTotalPrice in the UI.
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Current status of the order (e.g., "Confirmed", "Preparing", "Delivering").
        /// Internal status key.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Display text for the order status in Vietnamese (e.g., "Đã xác nhận", "Đang chuẩn bị").
        /// Maps to @id/tvStatus in the UI.
        /// </summary>
        public string StatusDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Next action that can be performed on this order (e.g., "MarkAsPreparing", "MarkAsReady").
        /// Used to determine which button to show in the UI.
        /// </summary>
        public string NextAction { get; set; } = string.Empty;
    }
}