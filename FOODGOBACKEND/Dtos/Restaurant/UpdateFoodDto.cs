using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Restaurant
{
    /// <summary>
    /// Request DTO for updating existing dish/food information.
    /// </summary>
    public class UpdateFoodDto
    {
        [Required(ErrorMessage = "Dish name is required.")]
        [StringLength(150, ErrorMessage = "Dish name cannot exceed 150 characters.")]
        public string DishName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 9999999999999999.99, ErrorMessage = "Price must be between 0 and 9999999999999999.99.")]
        public decimal Price { get; set; }

        /// <summary>
        /// Optional: Image file upload (jpg, jpeg, png, gif, webp - max 5MB).
        /// If not provided, existing image will be retained.
        /// </summary>
        public IFormFile? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}