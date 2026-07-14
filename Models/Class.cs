using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public virtual ICollection<CourseFee> CourseFees { get; set; } = new List<CourseFee>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
