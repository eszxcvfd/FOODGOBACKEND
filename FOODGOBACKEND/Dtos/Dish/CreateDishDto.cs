using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Dish
{
    public class CreateDishDto
    {
        [Required(ErrorMessage = "Dish name is required.")]
        [StringLength(150)]
        public string DishName { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}