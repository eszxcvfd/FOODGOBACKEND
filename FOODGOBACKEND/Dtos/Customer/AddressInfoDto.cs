namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Address information for order detail.
    /// </summary>
    public class AddressInfoDto
    {
        /// <summary>
        /// Name of the restaurant.
        /// </summary>
        public string RestaurantName { get; set; } = null!;

        /// <summary>
        /// Address of the restaurant.
        /// </summary>
        public string RestaurantAddress { get; set; } = null!;

        /// <summary>
        /// Delivery address for the order.
        /// </summary>
        public string DeliveryAddress { get; set; } = null!;

        /// <summary>
        /// Name of the customer.
        /// </summary>
        public string CustomerName { get; set; } = null!;

        /// <summary>
        /// Phone number of the customer.
        /// </summary>
        public string CustomerPhone { get; set; } = null!;
    }
}