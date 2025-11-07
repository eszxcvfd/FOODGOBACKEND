namespace FOODGOBACKEND.Dtos.Test
{
    /// <summary>
    /// DTO for testing address geocoding.
    /// </summary>
    public class GeocodeRequestDto
    {
        /// <summary>
        /// The address string to convert to coordinates.
        /// </summary>
        public string Address { get; set; } = null!;
    }
}