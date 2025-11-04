namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents a food item displayed to a customer.
    /// </summary>
    public class ItemFoodDto
    {
        /// <summary>
        /// The unique identifier for the dish.
        /// </summary>
        public int DishId { get; set; }

        /// <summary>
        /// The name of the dish.
        /// </summary>
        public string DishName { get; set; } = null!;

        /// <summary>
        /// The URL for the dish's image.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// The price of the dish.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// The average rating for the dish.
        /// </summary>
        public double AverageRating { get; set; }

        /// <summary>
        /// The total number of ratings the dish has received.
        /// </summary>
        public int RatingCount { get; set; }

        /// <summary>
        /// The total number of times this dish has been sold.
        /// </summary>
        public int TotalSold { get; set; }
    }
}