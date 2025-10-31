using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public int UserId { get; set; }

    public string? Title { get; set; }

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
