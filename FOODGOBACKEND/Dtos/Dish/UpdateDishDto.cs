using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Dish
{
    public class UpdateDishDto
    {
        [StringLength(150, ErrorMessage = "Dish name cannot exceed 150 characters.")]
        public string? DishName { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal? Price { get; set; }

        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public string? ImageUrl { get; set; }

        public bool? IsAvailable { get; set; }
    }
}