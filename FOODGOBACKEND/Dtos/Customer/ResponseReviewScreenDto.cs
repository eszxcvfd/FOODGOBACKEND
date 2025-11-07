namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Response DTO for the review screen, containing user information and current date.
    /// </summary>
    public class ResponseReviewScreenDto
    {
        /// <summary>
        /// The name of the user (customer).
        /// </summary>
        public string UserName { get; set; } = null!;

        /// <summary>
        /// The URL for the user's avatar image.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// The current date formatted as string (e.g., "02/11/2025").
        /// </summary>
        public string CurrentDate { get; set; } = null!;
    }
}