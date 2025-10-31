using FOODGOBACKEND.Dtos.Dish;

namespace FOODGOBACKEND.Dtos.Restaurant
{
    /// <summary>
    /// DTO for displaying detailed information about a restaurant.
    /// Used in Customer Use Case C-UC04: View restaurant details and menu.
    /// </summary>
    public class RestaurantDetailDto
    {
        /// <summary>
        /// The unique identifier for the restaurant.
        /// </summary>
        public int RestaurantId { get; set; }

        /// <summary>
        /// The name of the restaurant.
        /// </summary>
        public string RestaurantName { get; set; } = null!;

        /// <summary>
        /// The restaurant's full address.
        /// </summary>
        public string Address { get; set; } = null!;

        /// <summary>
        /// The restaurant's phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;

        /// <summary>
        /// Opening time of the restaurant.
        /// </summary>
        public TimeOnly? OpeningTime { get; set; }

        /// <summary>
        /// Closing time of the restaurant.
        /// </summary>
        public TimeOnly? ClosingTime { get; set; }

        /// <summary>
        /// Indicates whether the restaurant is currently active/open for orders.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When the restaurant was created/registered.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Average rating of the restaurant (calculated from reviews).
        /// </summary>
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Total number of reviews for the restaurant.
        /// </summary>
        public int? TotalReviews { get; set; }

        /// <summary>
        /// Total number of completed orders.
        /// </summary>
        public int? TotalOrders { get; set; }

        /// <summary>
        /// List of dishes/menu items offered by the restaurant.
        /// </summary>
        public List<DishDto> Dishes { get; set; } = new List<DishDto>();
    }
}