using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Request DTO for creating a new order.
    /// </summary>
    public class RequestOrderDto
    {
        /// <summary>
        /// The ID of the restaurant.
        /// </summary>
        [Required(ErrorMessage = "Restaurant ID is required.")]
        public int RestaurantId { get; set; }

        /// <summary>
        /// List of items in the order.
        /// </summary>
        [Required(ErrorMessage = "Order items are required.")]
        [MinLength(1, ErrorMessage = "At least one order item is required.")]
        public List<RequestOrderItemDto> Items { get; set; } = new List<RequestOrderItemDto>();
    }

    /// <summary>
    /// Represents an item in an order request.
    /// </summary>
    public class RequestOrderItemDto
    {
        /// <summary>
        /// The ID of the dish.
        /// </summary>
        [Required(ErrorMessage = "Dish ID is required.")]
        public int DishId { get; set; }

        /// <summary>
        /// The quantity of the dish.
        /// </summary>
        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; }
    }
}