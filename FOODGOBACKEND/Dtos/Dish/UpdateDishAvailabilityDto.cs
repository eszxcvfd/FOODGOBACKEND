using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Dish
{
    /// <summary>
    /// DTO for updating only the availability status of a dish.
    /// </summary>
    public class UpdateDishAvailabilityDto
    {
        /// <summary>
        /// The new availability status for the dish.
        /// Set to 'true' if the dish is available, 'false' otherwise.
        /// </summary>
        [Required(ErrorMessage = "The availability status is required.")]
        public bool IsAvailable { get; set; }
    }
}