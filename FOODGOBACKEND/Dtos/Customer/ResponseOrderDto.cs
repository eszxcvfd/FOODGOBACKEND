namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Response DTO for order creation.
    /// </summary>
    public class ResponseOrderDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string RestaurantName { get; set; } = null!;
        public string DeliveryAddress { get; set; } = null!;
        public string? Note { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<ResponseOrderItemDto> Items { get; set; } = new List<ResponseOrderItemDto>();
    }

    /// <summary>
    /// Represents an item in an order response.
    /// </summary>
    public class ResponseOrderItemDto
    {
        public int DishId { get; set; }
        public string DishName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal PriceAtOrder { get; set; }
        public decimal Total { get; set; }
    }
}