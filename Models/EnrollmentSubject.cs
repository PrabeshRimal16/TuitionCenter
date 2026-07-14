using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class EnrollmentSubject
{
    public int EnrollmentSubjectId { get; set; }

    public int EnrollmentId { get; set; }

    public int SubjectId { get; set; }

    public int? AssignedBatchId { get; set; }

    public virtual Batch? AssignedBatch { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
