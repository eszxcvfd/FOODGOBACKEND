using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Order
{
    /// <summary>
    /// DTO for creating a new order.
    /// Used in Customer Use Case C-UC05: Create order.
    /// </summary>
    public class CreateOrderDto
    {
        /// <summary>
        /// The ID of the restaurant to order from.
        /// </summary>
        [Required(ErrorMessage = "Restaurant ID is required.")]
        public int RestaurantId { get; set; }

        /// <summary>
        /// Delivery address for the order.
        /// </summary>
        [Required(ErrorMessage = "Delivery address is required.")]
        [StringLength(500, ErrorMessage = "Delivery address cannot exceed 500 characters.")]
        public string DeliveryAddress { get; set; } = null!;

        /// <summary>
        /// Optional note/instructions for the order.
        /// </summary>
        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }

        /// <summary>
        /// List of dishes to order with their quantities.
        /// </summary>
        [Required(ErrorMessage = "At least one dish must be ordered.")]
        [MinLength(1, ErrorMessage = "At least one dish must be ordered.")]
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();

        /// <summary>
        /// Optional voucher code to apply discount.
        /// </summary>
        [StringLength(20)]
        public string? VoucherCode { get; set; }
    }

    /// <summary>
    /// Represents a single item in the order.
    /// </summary>
    public class OrderItemDto
    {
        /// <summary>
        /// The ID of the dish to order.
        /// </summary>
        [Required(ErrorMessage = "Dish ID is required.")]
        public int DishId { get; set; }

        /// <summary>
        /// Quantity of this dish.
        /// </summary>
        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; }
    }
}