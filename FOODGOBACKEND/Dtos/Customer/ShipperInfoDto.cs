namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Shipper information for order detail.
    /// </summary>
    public class ShipperInfoDto
    {
        /// <summary>
        /// Full name of the shipper.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Phone number of the shipper.
        /// </summary>
        public string PhoneNumber { get; set; } = null!;
    }
}