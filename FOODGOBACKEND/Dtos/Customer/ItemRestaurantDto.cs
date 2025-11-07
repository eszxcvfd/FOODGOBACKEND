namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents a restaurant item displayed to a customer.
    /// </summary>
    public class ItemRestaurantDto
    {
        /// <summary>
        /// The unique identifier for the restaurant.
        /// </summary>
        public int RestaurantId { get; set; }

        /// <summary>
        /// The name of the restaurant.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The URL for the restaurant's image.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// The average rating for the restaurant (0-5).
        /// </summary>
        public double AverageRating { get; set; }

        /// <summary>
        /// The total number of reviews the restaurant has received.
        /// </summary>
        public int ReviewCount { get; set; }

        /// <summary>
        /// The total number of completed orders for the restaurant.
        /// </summary>
        public int CompletedOrderCount { get; set; }

        /// <summary>
        /// The distance from the user's location to the restaurant in kilometers.
        /// </summary>
        public double DistanceInKm { get; set; }
    }
}