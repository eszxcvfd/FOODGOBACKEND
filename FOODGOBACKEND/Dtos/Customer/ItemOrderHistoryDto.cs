namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents a summary of an order in the customer's order history.
    /// </summary>
    public class ItemOrderHistoryDto
    {
        /// <summary>
        /// The unique identifier for the order.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// The name of the restaurant where the order was placed.
        /// </summary>
        public string RestaurantName { get; set; } = null!;

        /// <summary>
        /// The current status of the order (e.g., Completed, Cancelled).
        /// </summary>
        public string OrderStatus { get; set; } = null!;

        /// <summary>
        /// The date and time when the order was placed.
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// The total price of the order.
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// A brief summary of the items in the order.
        /// </summary>
        public string OrderSummary { get; set; } = null!;
    }
}