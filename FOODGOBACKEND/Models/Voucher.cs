using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public string? Description { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal? MinOrderValue { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public int? MaxUsage { get; set; }

    public int? CurrentUsage { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();
}
