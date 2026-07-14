using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Announcement
{
    public int AnnouncementId { get; set; }

    public int BatchId { get; set; }

    public int TeacherId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual Batch Batch { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
