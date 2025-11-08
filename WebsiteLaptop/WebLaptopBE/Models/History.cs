using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class History
{
    public string HistoryId { get; set; } = null!;

    public string? ActivityType { get; set; }

    public string? Username { get; set; }

    public DateTime? Time { get; set; }

    public virtual Account? UsernameNavigation { get; set; }
}
