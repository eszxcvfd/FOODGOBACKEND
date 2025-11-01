using System;
using System.Collections.Generic;

namespace FOODGOBACKEND.Models;

public partial class UserDevice
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public int UserId { get; set; }

    public string DeviceToken { get; set; } = null!;

    public string DeviceType { get; set; } = null!;

    public string? DeviceModel { get; set; }

    public string? AppVersion { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime LastLogin { get; set; }

    public virtual User User { get; set; } = null!;
}
