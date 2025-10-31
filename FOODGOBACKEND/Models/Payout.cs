using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Payout
{
    public int PayoutId { get; set; }

    public int PartnerId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly PeriodStartDate { get; set; }

    public DateOnly PeriodEndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual User Partner { get; set; } = null!;
}
