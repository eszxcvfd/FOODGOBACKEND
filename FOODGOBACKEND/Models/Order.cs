using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int RestaurantId { get; set; }

    public int? ShipperId { get; set; }

    public string OrderCode { get; set; } = null!;

    public string DeliveryAddress { get; set; } = null!;

    public string? Note { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? PreparedAt { get; set; }

    public DateTime? DeliveringAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Restaurant Restaurant { get; set; } = null!;

    public virtual Shipper? Shipper { get; set; }
}
