using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class CourseType
{
    public int CourseTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public virtual ICollection<CourseFee> CourseFees { get; set; } = new List<CourseFee>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
