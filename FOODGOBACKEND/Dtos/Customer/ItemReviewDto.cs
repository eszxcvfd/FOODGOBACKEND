namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents a review item displayed to users.
    /// </summary>
    public class ItemReviewDto
    {
        /// <summary>
        /// The unique identifier for the review.
        /// </summary>
        public int ReviewId { get; set; }

        /// <summary>
        /// The name of the user who wrote the review.
        /// </summary>
        public string UserName { get; set; } = null!;

        /// <summary>
        /// The URL for the user's avatar image.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// The date when the review was created (formatted as string).
        /// </summary>
        public string ReviewDate { get; set; } = null!;

        /// <summary>
        /// The rating score (typically 1-5).
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// The review content/comment.
        /// </summary>
        public string? Content { get; set; }
    }
}