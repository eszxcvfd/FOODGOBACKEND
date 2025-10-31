namespace FOODGOBACKEND.Dtos.Dish
{
    /// <summary>
    /// Represents the data of a dish returned to the client.
    /// </summary>
    public class DishDto
    {
        /// <summary>
        /// The unique identifier for the dish.
        /// </summary>
        public int DishId { get; set; }

        /// <summary>
        /// The ID of the restaurant this dish belongs to.
        /// </summary>
        public int RestaurantId { get; set; }

        /// <summary>
        /// The name of the dish.
        /// </summary>
        public string DishName { get; set; } = null!;

        /// <summary>
        /// A description of the dish.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The price of the dish.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// The URL for the dish's image.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Indicates whether the dish is currently available.
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}