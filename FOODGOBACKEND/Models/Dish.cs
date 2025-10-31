using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Dish
{
    public int DishId { get; set; }

    public int RestaurantId { get; set; }

    public string DishName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Restaurant Restaurant { get; set; } = null!;
}
