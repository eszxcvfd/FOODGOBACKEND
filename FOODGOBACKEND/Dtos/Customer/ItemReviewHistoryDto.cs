namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents a review history item for a customer.
    /// Used to display reviews that the customer has written.
    /// </summary>
    public class ItemReviewHistoryDto
    {
        /// <summary>
        /// The unique identifier for the review.
        /// </summary>
        public int ReviewId { get; set; }

        /// <summary>
        /// The name of the dish that was reviewed.
        /// </summary>
        public string DishName { get; set; } = null!;

        /// <summary>
        /// The name of the restaurant where the dish belongs.
        /// </summary>
        public string RestaurantName { get; set; } = null!;

        /// <summary>
        /// The rating score given by the customer (typically 1-5).
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// The review comment/content written by the customer.
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// The date when the review was created (formatted as string).
        /// </summary>
        public string ReviewDate { get; set; } = null!;
    }
}