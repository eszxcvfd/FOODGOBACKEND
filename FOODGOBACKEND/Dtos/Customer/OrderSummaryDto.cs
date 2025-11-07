using System.Collections.Generic;

namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Financial summary of the order.
    /// </summary>
    public class OrderSummaryDto
    {
        /// <summary>
        /// List of items in the order.
        /// </summary>
        public List<OrderItemDetailDto> Items { get; set; } = new List<OrderItemDetailDto>();

        /// <summary>
        /// Subtotal (sum of all items before fees and discounts).
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Shipping fee.
        /// </summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Service fee (calculated).
        /// </summary>
        public decimal ServiceFee { get; set; }

        /// <summary>
        /// Total discount amount applied.
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Grand total amount to pay.
        /// </summary>
        public decimal GrandTotal { get; set; }

        /// <summary>
        /// Payment status text.
        /// </summary>
        public string PaymentStatusText { get; set; } = null!;
    }
}