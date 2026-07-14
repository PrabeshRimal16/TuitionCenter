using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class TimeSlot
{
    public int TimeSlotId { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Days { get; set; } = null!;

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
