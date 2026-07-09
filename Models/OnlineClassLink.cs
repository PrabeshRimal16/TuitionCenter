using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class OnlineClassLink
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public int TeacherId { get; set; }

    public string MeetingLink { get; set; } = null!;

    public DateOnly ClassDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ClassSchedule Schedule { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
