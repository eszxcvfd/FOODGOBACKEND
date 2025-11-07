namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents an item in the order detail.
    /// </summary>
    public class OrderItemDetailDto
    {
        /// <summary>
        /// Name of the dish.
        /// </summary>
        public string DishName { get; set; } = null!;

        /// <summary>
        /// Quantity ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Price of the dish at the time of order.
        /// </summary>
        public decimal PriceAtOrder { get; set; }
    }
}