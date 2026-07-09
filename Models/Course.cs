using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Course
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string CourseType { get; set; } = null!;

    public decimal Price { get; set; }

    public string? DurationInfo { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual Subject Subject { get; set; } = null!;
}
