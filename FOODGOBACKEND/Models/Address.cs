using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Address
{
    public int AddressId { get; set; }

    public int CustomerId { get; set; }

    public string Street { get; set; } = null!;

    public string? Ward { get; set; }

    public string? District { get; set; }

    public string? City { get; set; }

    public string FullAddress { get; set; } = null!;

    public bool IsDefault { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
