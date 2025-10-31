using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int DishId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtOrder { get; set; }

    public virtual Dish Dish { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual Review? Review { get; set; }
}
