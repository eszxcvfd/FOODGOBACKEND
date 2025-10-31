using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Restaurant
{
    public int RestaurantId { get; set; }

    public int OwnerId { get; set; }

    public string RestaurantName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public TimeOnly? OpeningTime { get; set; }

    public TimeOnly? ClosingTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User Owner { get; set; } = null!;
}
