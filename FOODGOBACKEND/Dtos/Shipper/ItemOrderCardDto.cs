using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Shipper
{
    /// <summary>
    /// DTO for displaying order card information in shipper's order list.
    /// </summary>
    public class ItemOrderCardDto
    {
        /// <summary>
        /// The unique identifier for the order.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Order code (e.g., "FO12345").
        /// Maps to @id/tvOrderCode in the UI.
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the order (e.g., "ĐANG GIAO", "ĐÃ GIAO").
        /// Maps to @id/tvStatus in the UI.
        /// </summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>
        /// Name of the restaurant.
        /// Maps to @id/tvRestaurantName in the UI.
        /// </summary>
        public string RestaurantName { get; set; } = string.Empty;

        /// <summary>
        /// Address of the restaurant.
        /// Maps to @id/tvAddress in the UI.
        /// </summary>
        public string RestaurantAddress { get; set; } = string.Empty;

        /// <summary>
        /// Total price of the order.
        /// Maps to @id/tvTotalPrice in the UI.
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Shipper's income/commission from this order.
        /// Maps to @id/tvIncome in the UI.
        /// </summary>
        public decimal Income { get; set; }
    }
}