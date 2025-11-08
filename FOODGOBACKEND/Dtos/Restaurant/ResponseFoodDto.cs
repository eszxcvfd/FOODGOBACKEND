namespace FOODGOBACKEND.Dtos.Restaurant
{
    /// <summary>
    /// Response DTO for dish/food information.
    /// </summary>
    public class ResponseFoodDto
    {
        public int DishId { get; set; }

        public int RestaurantId { get; set; }

        public string DishName { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; }
    }
}