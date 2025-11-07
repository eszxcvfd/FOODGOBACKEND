using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Request DTO for submitting a review for an order item.
    /// </summary>
    public class RequestReviewDto
    {
        /// <summary>
        /// The ID of the order item being reviewed.
        /// </summary>
        [Required(ErrorMessage = "Order item ID is required.")]
        public int OrderItemId { get; set; }

        /// <summary>
        /// The rating score (typically 1-5 stars).
        /// </summary>
        [Required(ErrorMessage = "Rating is required.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        /// <summary>
        /// The review comment/content.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Comment must not exceed 1000 characters.")]
        public string? Comment { get; set; }
    }
}