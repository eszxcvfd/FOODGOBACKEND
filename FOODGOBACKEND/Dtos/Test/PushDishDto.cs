using Microsoft.AspNetCore.Http;

namespace FOODGOBACKEND.Dtos.Test
{
    /// <summary>
    /// DTO for creating/pushing a new dish (testing purpose).
    /// </summary>
    public class PushDishDto
    {
        /// <summary>
        /// Restaurant ID that owns this dish.
        /// </summary>
        public int RestaurantId { get; set; }

        /// <summary>
        /// Dish name.
        /// </summary>
        public string DishName { get; set; } = null!;

        /// <summary>
        /// Dish description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Dish price.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Image file to upload.
        /// </summary>
        public IFormFile? ImageFile { get; set; }

        /// <summary>
        /// Is the dish available? Default: true.
        /// </summary>
        public bool IsAvailable { get; set; } = true;
    }
}