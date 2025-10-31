namespace FOODGOBACKEND.Dtos.Restaurant
{
    /// <summary>
    /// DTO for displaying summary information of a restaurant in a list.
    /// Used in Customer Use Case C-UC03: View list of restaurants.
    /// </summary>
    public class RestaurantSummaryDto
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
        /// The restaurant's address.
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
        /// Average rating of the restaurant (calculated from reviews).
        /// Optional field that can be populated if needed.
        /// </summary>
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Total number of reviews for the restaurant.
        /// Optional field that can be populated if needed.
        /// </summary>
        public int? TotalReviews { get; set; }
    }
}