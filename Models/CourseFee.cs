using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class CourseFee
{
    public int FeeId { get; set; }

    public int ClassId { get; set; }

    public int SubjectId { get; set; }

    public int CourseTypeId { get; set; }

    public decimal Amount { get; set; }

    public bool IsActive { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual CourseType CourseType { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
