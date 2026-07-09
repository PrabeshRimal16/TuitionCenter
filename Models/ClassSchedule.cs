using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class ClassSchedule
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int TeacherId { get; set; }

    public string CourseType { get; set; } = null!;

    public string DayOfWeek { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<OnlineClassLink> OnlineClassLinks { get; set; } = new List<OnlineClassLink>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
