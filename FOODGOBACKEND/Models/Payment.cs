using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
