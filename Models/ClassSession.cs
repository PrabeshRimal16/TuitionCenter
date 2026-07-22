using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class ClassSession
{
    public int SessionId { get; set; }

    public int BatchId { get; set; }

    public int TeacherId { get; set; }

    public string Title { get; set; } = null!;

    public string MeetingLink { get; set; } = null!;

    public DateOnly SessionDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = null!;

    public virtual Batch Batch { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
