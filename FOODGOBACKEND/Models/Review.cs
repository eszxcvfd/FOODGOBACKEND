using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int OrderItemId { get; set; }

    public int CustomerId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;
}
