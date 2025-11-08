using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Restaurant
{
    /// <summary>
    /// Request DTO for creating or updating dish/food information.
    /// Restaurant ID is automatically determined from authenticated user.
    /// </summary>
    public class RequestFoodDto
    {
        [Required(ErrorMessage = "Dish name is required.")]
        [StringLength(150, ErrorMessage = "Dish name cannot exceed 150 characters.")]
        public string DishName { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 9999999999999999.99, ErrorMessage = "Price must be between 0 and 9999999999999999.99.")]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public IFormFile? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}