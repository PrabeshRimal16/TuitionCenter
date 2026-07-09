using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Enrollment
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime EnrolledDate { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User Student { get; set; } = null!;
}
