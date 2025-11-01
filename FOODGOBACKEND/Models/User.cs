using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class User
{
    public int UserId { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payout> Payouts { get; set; } = new List<Payout>();

    public virtual Restaurant? Restaurant { get; set; }

    public virtual Shipper? Shipper { get; set; }

    public virtual ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();
}
