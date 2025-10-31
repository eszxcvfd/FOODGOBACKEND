using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Shipper
{
    public int ShipperId { get; set; }

    public string FullName { get; set; } = null!;

    public string? LicensePlate { get; set; }

    public bool IsAvailable { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User ShipperNavigation { get; set; } = null!;
}
