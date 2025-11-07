namespace FOODGOBACKEND.Dtos.Test
{
    /// <summary>
    /// DTO for testing distance calculation between two addresses.
    /// </summary>
    public class DistanceCalculationDto
    {
        /// <summary>
        /// Starting address.
        /// </summary>
        public string FromAddress { get; set; } = null!;

        /// <summary>
        /// Destination address.
        /// </summary>
        public string ToAddress { get; set; } = null!;
    }
}